///////////////////////////////////////////////////////////////////////////////
// UDPClient.fs : CUDPClientクラスの実装

namespace Stresser.CUDPClient


open Stresser
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections

// UDPによるデータグラムの送受信を行うクラス
type CUDPClient =
    inherit CStresser

    //-------------------------------------------------------------------------
    // メンバ変数

    // キャンセルトークン
    val m_Cancellation : CancellationTokenSource

    // ソケット
    val mutable m_ActiveSockets : Hashtable

    //-------------------------------------------------------------------------
    // コンストラクタ

    new ( argConfig : Config.CConfig ) =
        {
            inherit CStresser( argConfig ) 
            m_Cancellation = new CancellationTokenSource()
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

        // 通信を開始する
        Async.Start ( this.ConnectToServer , this.m_Cancellation.Token )

    /// 通信の停止
    override this.Stop () =
        base.Stop ()

        // キャンセルする
        this.m_Cancellation.Cancel ()

        // 現在生きているソケットをすべてクローズする
        lock ( this.m_ActiveSockets ) ( fun _ ->
            for itr in this.m_ActiveSockets do
                let itrSocket = ( itr :?> DictionaryEntry ).Value :?> Socket
                itrSocket.Close()
        )
        
    /// オブジェクトの種類を応答する
    override this.RunMode = Constant.UDPClient


    /// コネクション数を取得する
    override this.ConnectionCount
        with get () = 1u    // コネクション数という概念はないので、とりあえず1を返しておく

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// サーバに接続する
    member private this.ConnectToServer =
        // 通信を開始した時刻
        let comStartTime = DateTime.Now

        // サーバ側で区別するための乱数
        let cliIdent = 
            let wBuf = Array.zeroCreate( sizeof<uint64> )
            Random().NextBytes wBuf
            BitConverter.ToUInt64( wBuf, 0 )

        // 接続先アドレスを取得する
        let TargetAddress = this.rConfig.TargetAddress

        // 接続先ポート番号を取得する
        let TargetPortNo = this.rConfig.PortNumber

        // 帯域制御の引数
        let argTrafficShape =
            comStartTime,
            this.rConfig.UDPClient_MinBytesPerSec,
            this.rConfig.UDPClient_MaxBytesPerSec,
            this.rConfig.UDPClient_Wavelength,
            this.rConfig.UDPClient_Phase

        async {
            while true do
                try
                    // 送受信処理を開始する
                    let wAsyncproc =
                        [
                            yield this.RecvData this.NoticePerformData;
                            if not this.rConfig.UDPClient_ReceiveOnly then
                                yield this.SendData this.NoticePerformData argTrafficShape cliIdent
                        ]
                        |> Async.Parallel
                        |> Async.Catch
                    Async.RunSynchronously( wAsyncproc, Timeout.Infinite, this.m_Cancellation.Token )
                        |> function
                            | Choice1Of2 r -> ()
                            | Choice2Of2 e -> ()
                    
                with
                | _ as e ->
                    // 無視して強行する
                    ()
        }       

    // 受信処理
    member private this.RecvData noticeFunc =
        async {
            // 受信用のソケットを構築する
            use udpSock = new UdpClient( int this.rConfig.PortNumber )

            // 有効なソケットをHashtableに格納する
            lock ( this.m_ActiveSockets ) ( fun _ ->
                this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, udpSock.Client )
            )
            let s = udpSock.Client  // Socket

            // クライアントの識別子とカウンタを格納するコレクション
            let cliIdentCol = new Generic.Dictionary< uint64, uint64 >()

            try
                // 受信用のバッファを用意する
                let vBuffer : byte[] = Array.zeroCreate ( 65536 )

                // サーバから無限にゴミデータを受信し続ける
                // notcTim : 最後に集計結果を通知した時刻
                // recvSum : 最後に通知してからの受信バイト数
                // recvCount : 最後に通知してからの受信パケット数
                // dropCount : 最後に通知してからのドロップパケット数
                // jitTim : 最後にジッタを計算した時刻
                // jitCnt : 最後にジッタを計算してからの受信パケット数
                // lastLatency : 最後にジッタを計算したときのレイテンシ（平均値）
                // jitter : ジッタの最新値
                // maxCounter : 受信したカウンタの最大値
                let rec loop ( notcTim : DateTime ) recvSum recvCount dropCount ( jitTim : DateTime ) jitCnt lastLatency jitter =

                    // 受信する
                    let RecvLength = s.Receive vBuffer
                    if RecvLength = 0 then
                        raise <| Constant.DataReceiveError "Connection was closed by remote host."

                    // 受信終了時刻
                    let EndTime = DateTime.Now

                    // パケットのカウンタを取得
                    let recvCounter = BitConverter.ToUInt64( vBuffer, 0 )

                    // 識別子を取得
                    let cliIdent = BitConverter.ToUInt64( vBuffer, sizeof<uint64> )

                    // ドロップしたパケット数を算出する
                    let next_dropCount1 =
                        // 未知の識別子であれば初期値を登録する
                        if not <| cliIdentCol.ContainsKey( cliIdent ) then
                            cliIdentCol.Add( cliIdent, 0UL )
                        // 古い最大値を取得
                        let maxCounter = cliIdentCol.Item( cliIdent )
                        // 新しい最大値を設定する
                        cliIdentCol.Item( cliIdent ) <- max maxCounter recvCounter
                        if maxCounter + 1UL < recvCounter then
                            // 抜けが存在する
                            uint32( recvCounter - ( maxCounter + 1UL ) ) + dropCount
                        else
                            // 抜け漏れはない
                            dropCount

                    // 前回ジッタを計算してから、10ms以上経過していたら、ジッタの最新値を求める
                    let jitTimeSpan = EndTime.Subtract( jitTim ).TotalMilliseconds
                    let next_jitTim, next_jitCnt, next_lastLatency, next_jitter =
                        if jitTimeSpan > 10.0 then
                            // 前回ジッタを計算してからの経過時間と、受信パケット数をもとに、
                            // 受信間隔の平均値を出して、レイテンシとする
                            let currentLatency = jitTimeSpan / float ( jitCnt + 1u )

                            // jitterを計算する（RFC1889に従う）
                            let currentJitter = jitter + ( abs( currentLatency - lastLatency ) - jitter ) / 16.0

                            EndTime, 0u, currentLatency, currentJitter
                        else
                            jitTim, ( jitCnt + 1u ), lastLatency, jitter

                    // 最後に通知した時刻から100ms以上経過していたら、測定結果を集計する
                    let next_notcTim, next_recvSum, next_recvCount, next_dropCount2 =
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
                                    PIV_Jitter = next_jitter
                                    PIV_ReqCount = 0u
                                    PIV_ResCount = ( recvCount + 1u )
                                    PIV_DropCount = next_dropCount1
                                } : CurrentPerfomanceData.PerfomItemVal
                            )
                            EndTime, 0UL, 0u, 0u
                        else
                            notcTim, ( recvSum + uint64 RecvLength ), ( recvCount + 1u ), next_dropCount1
                
                    // 繰り返す
                    loop next_notcTim next_recvSum next_recvCount next_dropCount2 next_jitTim next_jitCnt next_lastLatency next_jitter

                loop DateTime.Now 0UL 0u 0u DateTime.Now 0u 0.0 0.0

            with
            | _ as e ->
                // 無視して強行する
                ()
                 
            // 無効になったソケットをHashtableから削除する
            lock ( this.m_ActiveSockets ) ( fun _ ->
                udpSock.Client.Close()   // クローズする
                this.m_ActiveSockets.Remove( box Thread.CurrentThread.ManagedThreadId )
            )                 
        }
    
    // 送信処理
    member private this.SendData noticeFunc argTrafficShape cliIdent =
        // 識別子のバイト配列
        let cliIdentArray = BitConverter.GetBytes cliIdent

        async {
            // 送信用のソケットを構築する
            use udpSock = new UdpClient( this.rConfig.TargetAddress, int this.rConfig.PortNumber )

            // 有効なソケットをHashtableに格納する
            lock ( this.m_ActiveSockets ) ( fun _ ->
                this.m_ActiveSockets.Add( box Thread.CurrentThread.ManagedThreadId, udpSock.Client )
            )
            let s = udpSock.Client  // Socket

            try
                // 送信用のバッファを用意する
                let vBuffer : byte[] = Array.zeroCreate ( 640 * 1024 )
                let bufRange = Generic.List< ArraySegment<byte> >( 1 )
                bufRange.Add( ArraySegment( vBuffer, 0, int this.rConfig.UDPClient_DatagramSize ) )

                // サーバに対して無限にゴミデータを送信し続ける
                let rec loop ( lastNoticeTime : DateTime ) totalByteCount sndCnt targetSpeed ( packetCounter : uint64 ) =

                    // 送信バッファの先頭に識別子とカウンタの値を設定する
                    let packetCounterArray = BitConverter.GetBytes packetCounter
                    Array.blit packetCounterArray 0 vBuffer 0 packetCounterArray.Length
                    Array.blit cliIdentArray 0 vBuffer packetCounterArray.Length cliIdentArray.Length

                    // 送る
                    let SentLength = s.Send bufRange
                
                    // 送信終了時刻
                    let EndTime = DateTime.Now

                    // 今までに送信したバイト数
                    let wSendByteCount = totalByteCount + uint64 SentLength

                    // 前回通知してからの経過時間(ms)
                    let wNoticeTimeSpan = EndTime.Subtract( lastNoticeTime ).TotalMilliseconds
                
                    let next_lastNoticeTime, next_totalByteCount, next_sndCnt, next_targetSpeed =
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
                                    PIV_ReqCount = sndCnt + 1u
                                    PIV_ResCount = 0u
                                    PIV_DropCount = 0u
                                } : CurrentPerfomanceData.PerfomItemVal
                            )
                            EndTime, 0UL, 0u, ( this.calcTargetSpeed argTrafficShape )

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

                            lastNoticeTime, wSendByteCount, ( sndCnt + 1u ), targetSpeed
                        
                    // 継続する
                    loop next_lastNoticeTime next_totalByteCount next_sndCnt next_targetSpeed ( packetCounter + 1UL )

                loop DateTime.Now 0UL 0u ( this.calcTargetSpeed argTrafficShape ) 1UL

            with
            | _ as e ->
                // 無視して強行する
                ()
                 
            // 無効になったソケットをHashtableから削除する
            lock ( this.m_ActiveSockets ) ( fun _ ->
                udpSock.Client.Close()   // クローズする
                this.m_ActiveSockets.Remove( box Thread.CurrentThread.ManagedThreadId )
            ) 
        }

    // 測定値の通知
    member private this.NoticePerformData ( argPD : CurrentPerfomanceData.PerfomItemVal ) =
        // 測定結果を集計する
        base.AddPerformanceData ( { argPD with PIV_Connections = this.ConnectionCount } )

    // 目標転送速度を算出する
    member private this.calcTargetSpeed argTrafficShape =
        // 引数をばらす
        let commStartTime,          // 通信開始時刻
            MinBytesPerSec,         // 最小帯域幅
            MaxBytesPerSec,         // 最大帯域幅
            Wavelength,             // 波長
            Phase =                 // 位相
              argTrafficShape

        let harf = float( ( MaxBytesPerSec - MinBytesPerSec ) / 2u )
        let mid = float MinBytesPerSec + harf

        // 経過時間(ms)
        let conTimeSpan = DateTime.Now.Subtract( commStartTime ).TotalMilliseconds

        // 帯域を求める
        // sinの引数は、波長:2π = (経過時間-位相):xで求める
        let w = Math.PI * ( conTimeSpan - ( float Phase * 1000.0 ) ) / ( float Wavelength * 1000.0 )
        let SpeedPerSec = sin( w ) * harf + mid

        // 100msあたりの目標送信バイト数を決定する
        uint64( SpeedPerSec / 10.0 )



