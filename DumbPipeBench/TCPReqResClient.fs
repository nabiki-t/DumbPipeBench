///////////////////////////////////////////////////////////////////////////////
// TCPReqResClient.fs : CTCPReqResClientクラスの実装
//

namespace Stresser.CTCPReqResClient

open Stresser
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections

/// 通信を行うクラスのインタフェースを規定する
type CTCPReqResClient =
    inherit CStresser

    //-------------------------------------------------------------------------
    // メンバ変数

    // サーバとの送受信処理に対する、キャンセルトークン
    val m_ServerConnect_Cancellation : CancellationTokenSource

    // サーバとの通信に使用しているソケット
    val mutable m_ActiveSockets : Hashtable

    //-------------------------------------------------------------------------
    // コンストラクタ

    new ( argConfig : Config.CConfig ) =
        {
            inherit CStresser( argConfig ) 
            m_ServerConnect_Cancellation = new CancellationTokenSource()
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

        // 使用するコネクション数を取得
        let ConnCnt = int this.rConfig.TCPReqResClient_MaxConnectionCount

        // スレッドを起動する際の待ち時間を算出する
        let waitTime = 
            if ConnCnt <= 1 then
                // コネクション数が1本であれば、待つ必要はない
                0.0
            else
                // 2本目以降の待ち時間（の間隔）を算出する
                ( double this.rConfig.TCPReqResClient_RampUpTime ) / ( double( ConnCnt - 1 ) )

        // コネクション数分、クライアントのスレッドを起動する
        for i = 0 to ConnCnt - 1 do
            let wWait = i * int( waitTime * 1000.0 )
            Async.Start ( this.ConnectToServer i wWait, this.m_ServerConnect_Cancellation.Token )

    /// 通信の停止
    override this.Stop () =
        base.Stop ()

        // キャンセルする
        this.m_ServerConnect_Cancellation.Cancel ()

        // 現在生きているソケットをすべてクローズする
        lock ( this.m_ActiveSockets ) ( fun _ ->
            for itr in this.m_ActiveSockets do
                let itrSocket = ( itr :?> DictionaryEntry ).Value :?> Socket
                itrSocket.Close()
        )
        
    /// オブジェクトの種類を応答する
    override this.RunMode = Constant.TCPReqResClient

    /// コネクション数を取得する
    override this.ConnectionCount
        with get () =
            // コネクション数を取得する
            // ※HashTableに対する、競合しうる更新処理はConnectToServerのみのため、ロックしなくてもスレッドセーフらしい
            uint32 this.m_ActiveSockets.Count

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// サーバに接続する
    member private this.ConnectToServer argIndex waitTime =
        // 接続先アドレスを取得する
        let TargetAddress = this.rConfig.TargetAddress

        // 接続先ポート番号を取得する
        let TargetPortNo = this.rConfig.PortNumber

        async {
            // スレッド開始後、一定時間待ち合わせる
            Thread.Sleep waitTime

            while true do
                try
                    // サーバに接続する
                    use cliSock =
                        new TcpClient(
                            TargetAddress,
                            int TargetPortNo,
                            NoDelay = this.rConfig.TCPReqResClient_DisableNagle,
                            ReceiveBufferSize = int32 this.rConfig.TCPReqResClient_ReceiveBufferSize,
                            SendBufferSize = int32 this.rConfig.TCPReqResClient_SendBufferSize
                        )
                        |> this.Negotiate

                    // 有効なソケットをHashtableに格納する
                    lock ( this.m_ActiveSockets ) ( fun _ ->
                        this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, cliSock.Client )
                    )

                    // サーバとの送受信処理
                    let wReqResFunc = this.ReqestAndResponceData cliSock.Client this.NoticePerformData
                    Async.RunSynchronously( Async.Catch <| wReqResFunc, Timeout.Infinite, this.m_ServerConnect_Cancellation.Token )
                    |> function
                        | Choice1Of2 r -> ()    // 正常終了
                        | Choice2Of2 e -> ()    // 例外が発生した

                    // 無効になったソケットをHashtableから削除する
                    lock ( this.m_ActiveSockets ) ( fun _ ->
                        cliSock.Client.Close()   // クローズする
                        this.m_ActiveSockets.Remove( box Thread.CurrentThread.ManagedThreadId )
                    )
                with
                | _ as e ->
                    // 無視して強行する
                    ()
        }

    // サーバとのネゴシエーション処理
    member private this.Negotiate s : TcpClient =
        let vSendData = "DumbPipeBench:TCPReqResClient"B   // 送信データ
        let vRecvData = "DumbPipeBench:TCPReqResServer"B   // 期待値
        let vRecvBuff : byte[] = Array.zeroCreate ( vRecvData.Length )   // 受信バッファ

        // DumbPipeBenchのTCPReqResクライアントであることがわかる文字列を送信する
        if s.Client.Send vSendData <> vSendData.Length then
            s.Close ()
            raise <| Constant.NegotiationError( "Failed to send magic word." )
        
        // DumbPipeBenchのTCPReqResサーバであることがわかる文字列を受信する
        if s.Client.Receive vRecvBuff <> vRecvData.Length || vRecvBuff <> vRecvData then
            s.Close ()
            raise <| Constant.NegotiationError( "Failed to receive magic word." )

        // 有効なソケットを返す
        s

    // サーバとの送受信処理
    member private this.ReqestAndResponceData ( s : Socket ) noticeFunc =
        async {
            // 送受信用のバッファを用意する
            let vBuffer : byte[] = Array.zeroCreate ( 64 * 1024 )
            let bufRange = Generic.List< ArraySegment<byte> >( 1 )

            // 要求・応答バイト長の範囲を取得する
            let MinReqLen = int32 <| this.rConfig.TCPReqResClient_MinReqestDataLength
            let MaxReqLen = int32 <| this.rConfig.TCPReqResClient_MaxReqestDataLength + 1u
            let MinResLen = int32 <| this.rConfig.TCPReqResClient_MinResponceDataLength
            let MaxResLen = int32 <| this.rConfig.TCPReqResClient_MaxResponceDataLength + 1u

            // 乱数源
            let rand = Random()

            // 要求および応答の送受信を行う
            // notcTim : 最後に集計結果の通知を行った時刻
            // sendSum : 最後に結果の集計を行ってからの、合計送信バイト数
            // recvSum : 最後に結果の集計を行ってからの、合計受信バイト数
            // sendCnt : 最後に結果の集計を行ってからの、送受信回数
            // rttCTim : 最後にRTTおよびジッタの計算を行った時刻
            // rttCnt : 最後にRTT・ジッタの計算を行ってからの、送受信回数
            // lastRTT : 最後に計算したRTT
            // jitter : ジッタの現在値
            let rec sendRecvLoop ( notcTim : DateTime ) sendSum recvSum sendCnt ( rttCTim : DateTime ) rttCnt lastRTT jitter =

                // 要求と応答のバイト長を決定する
                let ReqLen = rand.Next( MinReqLen, MaxReqLen )
                let ResLen = rand.Next( MinResLen, MaxResLen )

                // 要求と応答のバイト長を、送信データに設定する
                let ReqLenBytes = BitConverter.GetBytes ReqLen
                let ResLenBytes = BitConverter.GetBytes ResLen
                Array.blit ReqLenBytes 0 vBuffer 0 ReqLenBytes.Length
                Array.blit ResLenBytes 0 vBuffer ReqLenBytes.Length ResLenBytes.Length

                // 要求を送信する
                let rec sendLoop pos =
                    bufRange.Clear ()
                    bufRange.Add <| ArraySegment( vBuffer, pos, ReqLen - pos )
                    let SendLength = s.Send bufRange
                    if pos + SendLength < ReqLen then
                        sendLoop ( pos + SendLength )
                sendLoop 0

                // 応答を受信する
                let rec recvLoop pos =
                    bufRange.Clear ()
                    bufRange.Add <| ArraySegment( vBuffer, pos, ResLen - pos )
                    let RecvLength = s.Receive bufRange
                    if pos + RecvLength < ResLen then
                        recvLoop ( pos + RecvLength )
                recvLoop 0

                // 送信終了時刻
                let EndTime = DateTime.Now

                // 最後に集計結果を通知してからの経過時間
                let noticeSpan = EndTime.Subtract( notcTim ).TotalMilliseconds

                // 最後にRTTを算出してからの経過時間
                let RTTCompSpan = EndTime.Subtract( rttCTim ).TotalMilliseconds

                // 最後にRTTを算出してから10ms以上経過していたら、もう一度RTTを算出する
                let next_rttCTim, next_rttCnt, next_lastRTT, next_jitter =
                    if RTTCompSpan > 10.0 then
                        // 応答時間
                        let wCurrentRTT2 = RTTCompSpan / float( rttCnt + 1u )

                        // ジッタ
                        let currentJitter = jitter + ( abs( wCurrentRTT2 - lastRTT ) - jitter ) / 16.0

                        EndTime, 0u, wCurrentRTT2, currentJitter
                    else
                        // 更新しないのなら、前の値をそのまま次に引き継ぐ
                        rttCTim, ( rttCnt + 1u ), lastRTT, jitter 

                // 最後に通知した時刻から100ms以上経過していたら、測定結果を集計する
                let next_notcTim, next_sendSum, next_recvSum, next_sendCnt =
                    if noticeSpan > 100.0 then
                        noticeFunc (
                            {
                                PIV_ScanCount = 1u
                                PIV_Time = EndTime
                                PIV_Connections = 1u
                                PIV_TxBytes = uint64 ReqLen + sendSum
                                PIV_RxBytes = uint64 ResLen + recvSum
                                PIV_LatencySum = noticeSpan
                                PIV_LatencyCount = sendCnt + 1u         // 要求-応答の平均時間は、集計間隔（約100ms）に繰り返した回数をもとに算出する
                                PIV_Jitter = next_jitter                // ジッタは最新値を通知する
                                PIV_ReqCount = sendCnt + 1u             // 送受信した回数
                                PIV_ResCount = sendCnt + 1u             // 送受信した回数
                                PIV_DropCount = 0u
                            } : CurrentPerfomanceData.PerfomItemVal
                        )
                        // 次回の繰り返しに渡す、通知時刻や送受信バイト数・回数などを初期化する
                        EndTime, 0UL, 0UL, 0u
                    else
                        // 送受信バイト数や回数を加算して、もう一度繰り返す
                        notcTim, ( sendSum + uint64 ReqLen ), ( recvSum + uint64 ResLen ), ( sendCnt + 1u )
                
                // 繰り返す
                sendRecvLoop next_notcTim next_sendSum next_recvSum next_sendCnt next_rttCTim next_rttCnt next_lastRTT next_jitter

            sendRecvLoop DateTime.Now 0UL 0UL 0u DateTime.Now 0u 0.0 0.0

        }

    // 測定値の通知
    member private this.NoticePerformData ( argPD : CurrentPerfomanceData.PerfomItemVal ) =
        // 測定結果を集計する
        base.AddPerformanceData ( { argPD with PIV_Connections = this.ConnectionCount } )
