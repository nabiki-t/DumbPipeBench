///////////////////////////////////////////////////////////////////////////////
// TCPServer.fs : CTCPServerクラスの実装
//

namespace Stresser.CTCPServer

open Stresser

open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections

/// 通信を行うクラスのインタフェースを規定する
type CTCPServer =
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

        // 通信を開始した時刻
        let connStartTime = DateTime.Now

        // 念のため、有効なソケットの情報をすべて破棄する
        this.m_ActiveSockets.Clear()

        // TCPリスナを構築
        try
            this.m_TcpListener <- System.Net.Sockets.TcpListener.Create  ( int this.rConfig.PortNumber )
            this.m_TcpListener.Start ()
        with
        | _ as e ->
            System.Windows.MessageBox.Show "Faild to open TCP port" |> ignore
        
        // 非同期処理を開始してやる
        Async.Start ( this.WaitForClient connStartTime, this.m_WaitForClient_Cancellation.Token )

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
    override this.RunMode = Constant.TCPServer

    /// コネクション数を取得する
    override this.ConnectionCount
        with get () =
            // コネクション数を取得する
            // ※HashTableに対する、競合しうる更新処理はConnectToServerのみのため、ロックしなくてもスレッドセーフらしい
            uint32 this.m_ActiveSockets.Count

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// クライアントからの接続を待ち受け、データの送受信を行う
    member private this.WaitForClient connStartTime =
        async {
            try
                while true do
                    Async.Start(
                        this.m_TcpListener.AcceptSocket ()
                            |> this.ProcClientRequest connStartTime,
                        this.m_WaitForClient_Cancellation.Token
                    )
             with
             | _ -> ()  // 握りつぶす
             // ※終了するときに、AcceptSocketで例外が発生するため
        }

    // クライアントとの送受信処理
    member private this.ProcClientRequest connStartTime ( s : Socket ) =
        // 帯域制御の引数
        let argTrafficShape =
            connStartTime,
            this.rConfig.TCPServer_EnableTrafficShaping,
            this.rConfig.TCPServer_MinBytesPerSec,
            this.rConfig.TCPServer_MaxBytesPerSec,
            this.rConfig.TCPServer_Wavelength,
            this.rConfig.TCPServer_Phase

        async {
            try
                // 設定値を指定する
                s.NoDelay <- this.rConfig.TCPServer_DisableNagle
                s.ReceiveBufferSize <- int32 this.rConfig.TCPServer_ReceiveBufferSize
                s.SendBufferSize <- int32 this.rConfig.TCPServer_SendBufferSize

                //s.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.UseLoopback, true )

                // ネゴシエーション
                this.Negotiate s

                // 有効なソケットをHashtableに格納する
                lock ( this.m_ActiveSockets ) ( fun _ ->
                    this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, s )
                )

                // クライアントに対する送受信処理を開始する
                let wAsyncproc =
                    [
                        // 送受信処理そのものはTCPClientのものをそのまま使用する
                        yield CTCPClient.CTCPClient.RecvData s this.NoticePerformData;
                        if not this.rConfig.TCPServer_ReceiveOnly then
                            yield CTCPClient.CTCPClient.SendData s this.NoticePerformData argTrafficShape
                    ]
                    |> Async.Parallel
                    |> Async.Catch
                Async.RunSynchronously( wAsyncproc, Timeout.Infinite, this.m_WaitForClient_Cancellation.Token )
                    |> function
                        | Choice1Of2 r -> ()    // 正常終了
                        | Choice2Of2 e -> ()    // 例外が発生した
            
                // 無効になったソケットをHashtableから削除する
                lock ( this.m_ActiveSockets ) ( fun _ ->
                    s.Close()   // クローズする
                    this.m_ActiveSockets.Remove( box Thread.CurrentThread.ManagedThreadId )
                )
            with
            | _ as e -> ()  // 握りつぶす
        }

    // クライアントとのネゴシエーション処理
    member private this.Negotiate s =
        let vSendData = "DumbPipeBench:TCPServer"B   // 送信データ
        let vRecvData = "DumbPipeBench:TCPClient"B   // 期待値
        let vRecvBuff : byte[] = Array.zeroCreate vRecvData.Length   // 受信バッファ

        // DumbPipeBenchのTCPクライアントであることがわかる文字列を受信する
        if s.Receive vRecvBuff <> vRecvData.Length || vRecvBuff <> vRecvData then
            s.Close ()
            raise <| Constant.NegotiationError "Failed to receive magic word."

        // DumbPipeBenchのTCPサーバであることがわかる文字列を送信する
        if s.Send vSendData <> vSendData.Length then
            s.Close ()
            raise <| Constant.NegotiationError "Failed to send magic word."

    // 測定値の通知
    member private this.NoticePerformData ( argPD : CurrentPerfomanceData.PerfomItemVal ) =
        // 測定結果を集計する
        base.AddPerformanceData ( { argPD with PIV_Connections = this.ConnectionCount } )
