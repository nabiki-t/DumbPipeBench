///////////////////////////////////////////////////////////////////////////////
// TCPReqResServer.fs : CTCPReqResServerクラスの実装
//

namespace Stresser.CTCPReqResServer

open Stresser

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections

/// 通信を行うクラスのインタフェースを規定する
type CTCPReqResServer =
    inherit CStresser

    //-------------------------------------------------------------------------
    // メンバ変数

    // TCPリスナ
    val mutable m_TcpListener : Sockets.TcpListener

    // クライアントからの接続待ち受け処理、およびクライアントとの送受信処理に対する、キャンセルトークン
    val m_WaitForClient_Cancellation : CancellationTokenSource

    // クライアントとの通信に使用しているソケット
    val mutable m_ActiveSockets : Hashtable

    //-------------------------------------------------------------------------
    // コンストラクタ

    new ( argConfig : Config.CConfig ) =
        {
            inherit CStresser( argConfig )
            m_TcpListener = null
            m_WaitForClient_Cancellation = new CancellationTokenSource()
            m_ActiveSockets = new Hashtable()
        }

    //-------------------------------------------------------------------------
    // メソッド

    /// 初期化
    override this.Initialize () =
        base.Initialize ()

    /// 破棄
    override this.Destroy () =
        base.Destroy ()

    /// 通信の開始
    override this.Start () =
        base.Start ()

        // 念のため、有効なソケットの情報をすべて破棄する
        this.m_ActiveSockets.Clear()

        // TCPリスナを構築
        try
            this.m_TcpListener <- new TcpListener ( IPAddress.Any,int this.rConfig.PortNumber)
            this.m_TcpListener.Start ()
        with
        | _ as e ->
            System.Windows.MessageBox.Show "Faild to open TCP port" |> ignore
        
        // 非同期処理を開始してやる
        Async.Start ( this.WaitForClient (), this.m_WaitForClient_Cancellation.Token )

    /// 通信の停止
    override this.Stop () =
        base.Stop ()
        
        // まず、受信を待機しているTCPリスナーを停止する
        this.m_TcpListener.Stop ()

        // キャンセルする
        this.m_WaitForClient_Cancellation.Cancel ()

        // クライアントとの通信を行っているソケットをすべて破棄する
        lock ( this.m_ActiveSockets ) ( fun _ ->
            for itr in this.m_ActiveSockets do
                ( ( itr :?> DictionaryEntry ).Value :?> Socket ).Close()
        )

    /// オブジェクトの種類を応答する
    override this.RunMode = Constant.TCPReqResServer

    /// コネクション数を取得する
    override this.ConnectionCount
        with get () =
            // コネクション数を取得する
            // ※HashTableに対する、競合しうる更新処理はConnectToServerのみのため、ロックしなくてもスレッドセーフらしい
            uint32 this.m_ActiveSockets.Count

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// クライアントからの接続を待ち受け、データの送受信を行う
    member private this.WaitForClient () =
        async {
            try
                while true do
                    Async.Start(
                        this.m_TcpListener.AcceptSocket ()
                            |> this.ProcClientRequest,
                        this.m_WaitForClient_Cancellation.Token
                    )
             with
             | _ -> ()  // 握りつぶす
             // ※終了するときに、AcceptSocketで例外が発生するため
        }

    // クライアントとの送受信処理
    member private this.ProcClientRequest ( s : Socket ) =
        async {
            try
                // 設定値を指定する
                s.NoDelay <- this.rConfig.TCPReqResServer_DisableNagle
                s.ReceiveBufferSize <- int32 this.rConfig.TCPReqResServer_ReceiveBufferSize
                s.SendBufferSize <- int32 this.rConfig.TCPReqResServer_SendBufferSize

                // ネゴシエーション
                this.Negotiate s

                // 有効なソケットをHashtableに格納する
                lock ( this.m_ActiveSockets ) ( fun _ ->
                    this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, s )
                )

                // クライアントに対する送受信処理を開始する
                do! this.ReqestAndResponceData s this.NoticePerformData
            
            with
            | _ as e -> ()  // 握りつぶす

            // 無効になったソケットをHashtableから削除する
            lock ( this.m_ActiveSockets ) ( fun _ ->
                s.Close()   // クローズする
                this.m_ActiveSockets.Remove( box Thread.CurrentThread.ManagedThreadId )
            )
        }

    // クライアントとのネゴシエーション処理
    member private this.Negotiate s =
        let vSendData = "DumbPipeBench:TCPReqResServer"B   // 送信データ
        let vRecvData = "DumbPipeBench:TCPReqResClient"B   // 期待値
        let vRecvBuff : byte[] = Array.zeroCreate vRecvData.Length   // 受信バッファ

        // DumbPipeBenchのTCPReqResクライアントであることがわかる文字列を受信する
        if s.Receive vRecvBuff <> vRecvData.Length || vRecvBuff <> vRecvData then
            s.Close ()
            raise <| Constant.NegotiationError "Failed to receive magic word."

        // DumbPipeBenchのTCPReqResサーバであることがわかる文字列を送信する
        if s.Send vSendData <> vSendData.Length then
            s.Close ()
            raise <| Constant.NegotiationError "Failed to send magic word."


    // クライアントとの送受信処理
    member private this.ReqestAndResponceData ( s : Socket ) noticeFunc =
        async {
            // 送受信用のバッファを用意する
            let vBuffer : byte[] = Array.zeroCreate ( 64 * 1024 )

            // 要求および応答の送受信を行う
            let rec sendRecvLoop ( notcTim : DateTime ) sendSum recvSum recvCnt =

                // 要求バイト長を取得する
                CTCPReqResServer.recvLoop s vBuffer 0 sizeof<int>
                let ReqLen = BitConverter.ToInt32( vBuffer, 0 )

                // 応答バイト長を取得する
                CTCPReqResServer.recvLoop s  vBuffer 0 sizeof<int>
                let ResLen = BitConverter.ToInt32( vBuffer, 0 )

                // 残りのバイトを取得する
                CTCPReqResServer.recvLoop s vBuffer 0 ( ReqLen - sizeof<int> * 2 )

                // 応答のデータを送信する
                CTCPReqResServer.sendLoop s vBuffer 0 ResLen

                // 送信終了時刻
                let EndTime = DateTime.Now

                // 最後に通知した時刻から100ms以上経過していたら、測定結果を集計する
                if EndTime.Subtract( notcTim ).TotalMilliseconds > 100.0 then
                    noticeFunc (
                        {
                            PIV_ScanCount = 1u
                            PIV_Time = EndTime
                            PIV_Connections = 1u
                            PIV_TxBytes = uint64 ResLen + sendSum
                            PIV_RxBytes = uint64 ReqLen + recvSum
                            PIV_LatencySum = 0.0
                            PIV_LatencyCount = 0u
                            PIV_Jitter = 0.0
                            PIV_ReqCount = recvCnt + 1u
                            PIV_ResCount = recvCnt + 1u
                            PIV_DropCount = 0u
                        } : CurrentPerfomanceData.PerfomItemVal
                    )
                    sendRecvLoop EndTime 0UL 0UL 0u
                else
                    sendRecvLoop notcTim ( sendSum + uint64 ResLen ) ( recvSum + uint64 ReqLen ) ( recvCnt + 1u )

            sendRecvLoop DateTime.Now 0UL 0UL 0u
        }

    // 送信する
    static member private sendLoop s vBuf pos len =
        let w = Generic.List< ArraySegment<byte> >( 1 )
        w.Add <| ArraySegment( vBuf, pos, len - pos )
        let SendLength = s.Send w
        if pos + SendLength < len then
            CTCPReqResServer.sendLoop s vBuf ( pos + SendLength ) len

    // 受信する
    static member private recvLoop s vBuf pos len =
        let w = Generic.List< ArraySegment<byte> >( 1 )
        w.Add <| ArraySegment( vBuf, pos, len - pos )
        let RecvLength = s.Receive w
        if RecvLength = 0 then raise <| Constant.DataReceiveError "Connection was closed by remote host."
        if pos + RecvLength < len then
            CTCPReqResServer.recvLoop s vBuf ( pos + RecvLength ) len

    // 測定値の通知
    member private this.NoticePerformData ( argPD : CurrentPerfomanceData.PerfomItemVal ) =
        // 測定結果を集計する
        base.AddPerformanceData ( { argPD with PIV_Connections = this.ConnectionCount } )
