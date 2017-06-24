///////////////////////////////////////////////////////////////////////////////
// TCPClient.fs : CTCPClientクラスの実装

namespace Stresser.CTCPClient

open Stresser
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections

/// 通信を行うクラスのインタフェースを規定する
type CTCPClient =
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

        // 通信を開始した時刻
        let connStartTime = DateTime.Now

        // 念のため、有効なソケットの情報をすべて破棄する
        this.m_ActiveSockets.Clear()

        // 使用するコネクション数を取得
        let ConnCnt = int this.rConfig.TCPClient_MaxConnectionCount

        // スレッドを起動する際の待ち時間を算出する
        let waitTime = 
            if ConnCnt <= 1 then
                // コネクション数が1本であれば、待つ必要はない
                0.0
            else
                // 2本目以降の待ち時間（の間隔）を算出する
                ( double this.rConfig.TCPClient_RampUpTime ) / ( double( ConnCnt - 1 ) )

        // コネクション数分、クライアントのスレッドを起動する
        for i = 0 to ConnCnt - 1 do
            let wWait = i * int( waitTime * 1000.0 )
            Async.Start ( this.ConnectToServer i wWait connStartTime, this.m_ServerConnect_Cancellation.Token )

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
    override this.RunMode = Constant.TCPClient

    /// コネクション数を取得する
    override this.ConnectionCount
        with get () =
            // コネクション数を取得する
            // ※HashTableに対する、競合しうる更新処理はConnectToServerのみのため、ロックしなくてもスレッドセーフらしい
            uint32 this.m_ActiveSockets.Count

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// サーバに接続する
    member private this.ConnectToServer argIndex waitTime connStartTime =
        // 接続先アドレスを取得する
        let TargetAddress = this.rConfig.TargetAddress

        // 接続先ポート番号を取得する
        let TargetPortNo = this.rConfig.PortNumber

        // 帯域制御の引数
        let argTrafficShape =
            connStartTime,
            this.rConfig.TCPClient_EnableTrafficShaping,
            this.rConfig.TCPClient_MinBytesPerSec,
            this.rConfig.TCPClient_MaxBytesPerSec,
            this.rConfig.TCPClient_Wavelength,
            this.rConfig.TCPClient_Phase

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
                            NoDelay = this.rConfig.TCPClient_DisableNagle,
                            ReceiveBufferSize = int32 this.rConfig.TCPClient_ReceiveBufferSize,
                            SendBufferSize = int32 this.rConfig.TCPClient_SendBufferSize
                        )
                        |> this.Negotiate
                    
                    //cliSock.Client.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.UseLoopback, true )

                    // 有効なソケットをHashtableに格納する
                    lock ( this.m_ActiveSockets ) ( fun _ ->
                        this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, cliSock.Client )
                    )

                    // サーバに対する送受信処理を開始する
                    let wAsyncproc =
                        [
                            yield CTCPClient.RecvData cliSock.Client this.NoticePerformData;
                            if not this.rConfig.TCPClient_ReceiveOnly then
                                yield CTCPClient.SendData cliSock.Client this.NoticePerformData argTrafficShape
                        ]
                        |> Async.Parallel
                        |> Async.Catch
                    Async.RunSynchronously( wAsyncproc, Timeout.Infinite, this.m_ServerConnect_Cancellation.Token )
                        |> function
                            | Choice1Of2 r -> ()
                            | Choice2Of2 e -> ()

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
        let vSendData = "DumbPipeBench:TCPClient"B   // 送信データ
        let vRecvData = "DumbPipeBench:TCPServer"B   // 期待値
        let vRecvBuff : byte[] = Array.zeroCreate ( vRecvData.Length )   // 受信バッファ

        // DumbPipeBenchのTCPクライアントであることがわかる文字列を送信する
        if s.Client.Send vSendData <> vSendData.Length then
            s.Close ()
            raise <| Constant.NegotiationError( "Failed to send magic word." )
        
        // DumbPipeBenchのTCPサーバであることがわかる文字列を受信する
        if s.Client.Receive vRecvBuff <> vRecvData.Length || vRecvBuff <> vRecvData then
            s.Close ()
            raise <| Constant.NegotiationError( "Failed to receive magic word." )

        // 有効なソケットを返す
        s

    // サーバからの受信処理
    // ※サーバ側におけるクライアントへの受信処理でも使用する
    static member public RecvData ( s : Socket ) noticeFunc =
        async {
            // 受信用のバッファを用意する
            let vBuffer : byte[] = Array.zeroCreate ( 640 * 1024 )
            let bufRange = Generic.List< ArraySegment<byte> >( 1 )
            bufRange.Add( ArraySegment( vBuffer, 0, vBuffer.Length ) )

            // サーバから無限にゴミデータを受信し続ける
            let rec loop ( notcTim : DateTime ) recvSum pos =

                // 受信する
                bufRange.Item( 0 ) <- ArraySegment( vBuffer, pos, vBuffer.Length - pos )
                let RecvLength = s.Receive bufRange
                if RecvLength = 0 then
                    raise <| Constant.DataReceiveError "Connection was closed by remote host."
                
                // 受信終了時刻
                let EndTime = DateTime.Now

                // 最後に通知した時刻から100ms以上経過していたら、測定結果を集計する
                let next_notcTim, next_recvSum =
                    if EndTime.Subtract( notcTim ).TotalMilliseconds > 100.0 then
                        noticeFunc (
                            {
                                PIV_ScanCount = 1u
                                PIV_Time = EndTime
                                PIV_Connections = 1u
                                PIV_TxBytes = 0UL
                                PIV_RxBytes =  recvSum + uint64 RecvLength
                                PIV_LatencySum = 0.0
                                PIV_LatencyCount = 0u
                                PIV_Jitter = 0.0
                                PIV_ReqCount = 0u
                                PIV_ResCount = 0u
                                PIV_DropCount = 0u
                            } : CurrentPerfomanceData.PerfomItemVal
                        )
                        EndTime, 0UL
                    else
                        notcTim, ( recvSum + uint64 RecvLength )
                
                // 繰り返す
                loop next_notcTim next_recvSum ( ( pos + RecvLength ) % vBuffer.Length ) 

            loop DateTime.Now 0UL 0 0.0 0u 0.0 0.0
        }
    
    // サーバへの送信処理
    // ※サーバ側におけるクライアントへの送信処理でも使用する
    static member public SendData ( s : Socket ) noticeFunc argTrafficShape =
        async {
            // 送信用のバッファを用意する
            let vBuffer : byte[] = Array.zeroCreate ( 640 * 1024 )
            let bufRange = Generic.List< ArraySegment<byte> >( 1 )
            bufRange.Add( ArraySegment( vBuffer, 0, vBuffer.Length ) )

            // サーバに対して無限にゴミデータを送信し続ける
            let rec loop ( lastNoticeTime : DateTime ) totalByteCount pos targetSpeed =
                
                // 送信すべきバイト数を決定する
                let SendDataLength = min ( uint64 vBuffer.Length - uint64 pos ) ( targetSpeed - totalByteCount )

                // 送る
                let SentLength =
                    // 送信すべきデータが1バイト以上ある場合のみ送信する
                    if SendDataLength >= 1UL then
                        bufRange.Item(0) <- ArraySegment( vBuffer, pos, int SendDataLength )
                        s.Send bufRange
                    else
                        0
                
                // 送信終了時刻
                let EndTime = DateTime.Now

                // 今までに送信したバイト数
                let wSendByteCount = totalByteCount + uint64 SentLength

                // 前回通知してからの経過時間(ms)
                let wNoticeTimeSpan = EndTime.Subtract( lastNoticeTime ).TotalMilliseconds
                
                let next_lastNoticeTime, next_totalByteCount, next_targetSpeed =
                    if wNoticeTimeSpan > 100.0 then
                        // 最後に通知した時刻から100ms以上経過していたら、測定結果を集計する
                        noticeFunc (
                            {
                                PIV_ScanCount = 1u
                                PIV_Time = EndTime
                                PIV_Connections = 1u
                                PIV_TxBytes = wSendByteCount
                                PIV_RxBytes = 0UL
                                PIV_LatencySum = 0.0
                                PIV_LatencyCount = 0u
                                PIV_Jitter = 0.0
                                PIV_ReqCount = 0u
                                PIV_ResCount = 0u
                                PIV_DropCount = 0u
                            } : CurrentPerfomanceData.PerfomItemVal
                        )
                        EndTime, 0UL, ( CTCPClient.calcTargetSpeed argTrafficShape )

                    else
                        // 集計結果の通知を行わない場合は、一定時間の待ちを置く

                        // 次に通知するまでの残り時間
                        let wRestTime = 100.0 - wNoticeTimeSpan

                        // 100msあたりの目標送信バイト数分以上送信してしまっていたら、
                        // 残り時間分は全部待ち合わせる
                        if wSendByteCount >= targetSpeed then
                            Thread.Sleep ( int wRestTime )
                        else
                            // 1msあたりの送信バイト数
                            let OneMS_ByteCount = ( float wSendByteCount ) / wNoticeTimeSpan
                            // 残り時間での、送信可能予測バイト数
                            let MaxCapacityByteCount = wRestTime * OneMS_ByteCount
                            // 残り時間で送信すべきバイト数
                            let NeedsByteCount = float( targetSpeed - wSendByteCount )

                            if NeedsByteCount < MaxCapacityByteCount then
                                // 速度が速いため、ある程度の待ち時間が必要
                                if ( MaxCapacityByteCount - NeedsByteCount ) > OneMS_ByteCount * 10.0 then
                                    // 10ms程度であれば余裕があると思われる
                                    Thread.Sleep 10

                        lastNoticeTime, wSendByteCount, targetSpeed
                        
                // 継続する
                loop next_lastNoticeTime next_totalByteCount ( ( pos + SentLength ) % vBuffer.Length ) next_targetSpeed

            loop DateTime.Now 0UL 0 ( CTCPClient.calcTargetSpeed argTrafficShape )
        }

    // 測定値の通知
    member private this.NoticePerformData ( argPD : CurrentPerfomanceData.PerfomItemVal ) =
        // 測定結果を集計する
        base.AddPerformanceData ( { argPD with PIV_Connections = this.ConnectionCount } )

    // 目標転送速度を算出する
    static member private calcTargetSpeed argTrafficShape =
        // 引数をばらす
        let connStartTime,          // 通信開始時刻
            EnableTrafficShaping,   // 帯域制御の要否
            MinBytesPerSec,         // 最小帯域幅
            MaxBytesPerSec,         // 最大帯域幅
            Wavelength,             // 波長
            Phase =                 // 位相
              argTrafficShape

        if EnableTrafficShaping then
            let harf = float( ( MaxBytesPerSec - MinBytesPerSec ) / 2u )
            let mid = float MinBytesPerSec + harf

            // 経過時間(ms)
            let conTimeSpan = DateTime.Now.Subtract( connStartTime ).TotalMilliseconds

            // 帯域を求める
            // sinの引数は、波長:2π = (経過時間-位相):xで求める
            let w = Math.PI * ( conTimeSpan - ( float Phase * 1000.0 ) ) / ( float Wavelength * 1000.0 )
            let SpeedPerSec = sin( w ) * harf + mid

            // 100msあたりの目標送信バイト数を決定する
            uint64( SpeedPerSec / 10.0 )
        else
            // 帯域制御を行わない場合は、とりあえず十分に大きな値を指定されたものとして取り扱う
            UInt64.MaxValue


