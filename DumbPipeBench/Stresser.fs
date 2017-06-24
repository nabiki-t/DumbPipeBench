///////////////////////////////////////////////////////////////////////////////
// Stresser.fs : Stresserインタフェースの宣言


namespace Stresser

open System.Collections
open CurrentPerfomanceData

/// 測定の状態を示す
type RunningStatus =
    | Running
    | NotRunning

/// 通信を行うクラスのインタフェースを規定する
[<AbstractClass>]
type CStresser =

    //-------------------------------------------------------------------------
    // メンバ変数

    // 設定値を保持する
    val rConfig : Config.CConfig

    // 測定値
    val rPerformanceData : SortedList

    // 実行中か否か
    val mutable m_RunningStat : RunningStatus

    //-------------------------------------------------------------------------
    // コンストラクタ

    new ( argConfig : Config.CConfig ) =
        {
            rConfig = argConfig
            rPerformanceData = new SortedList()
            m_RunningStat = NotRunning
        }

    //-------------------------------------------------------------------------
    // 公開メソッド


    /// 初期化
    abstract Initialize : unit -> unit
    default this.Initialize () =
        // 測定値をすべて破棄する
        this.rPerformanceData.Clear()
        this.m_RunningStat <- NotRunning
        ()

    /// 破棄
    abstract Destroy : unit -> unit
    default this.Destroy () =
        // 測定値をすべて破棄する
        this.rPerformanceData.Clear()
        ()

    /// 通信の開始
    abstract Start : unit -> unit
    default this.Start () =
        // 測定値をすべて破棄する
        this.rPerformanceData.Clear()

        // 実行中状態に変える
        this.m_RunningStat <- Running
        ()

    /// 通信の停止
    abstract Stop : unit -> unit
    default this.Stop () =
        // 測定値をすべて破棄する
        this.rPerformanceData.Clear()

        // 未実行状態に変える
        this.m_RunningStat <- NotRunning
        ()


    /// オブジェクトの種類を応答する
    abstract RunMode : Constant.RunMode

    /// コネクション数を取得する
    abstract ConnectionCount : uint32 with get

    /// 測定値を加算する
    member public this.AddPerformanceData argData =

        // 実行中でなければ何もしない
        if this.m_RunningStat <> Running then
            ()
        else
            // 検索用のキーを生成する（秒単位で集計したい）
            let sk =
                sprintf
                    "%04d%02d%02d%02d%02d%02d"
                    argData.PIV_Time.Year
                    argData.PIV_Time.Month
                    argData.PIV_Time.Day
                    argData.PIV_Time.Hour
                    argData.PIV_Time.Minute
                    argData.PIV_Time.Second

            lock ( this.rPerformanceData ) ( fun _ ->

                // 同一キーのレコードが存在するか、探す
                let rOldRec = this.rPerformanceData.Item( sk )

                if rOldRec = null then
                    // 同一キーのレコードがなければ、引数に指定されたものをリストに追加して終了
                    this.rPerformanceData.Add( sk, argData )
                else
                    // 同一キーのレコードが存在する場合は、既存のものに値を加算する
                    let rOldPIVRec = rOldRec :?> PerfomItemVal
                    this.rPerformanceData.Remove sk
                    this.rPerformanceData.Add(
                        sk,
                        {
                            PIV_ScanCount = rOldPIVRec.PIV_ScanCount + argData.PIV_ScanCount
                            PIV_Time = rOldPIVRec.PIV_Time
                            PIV_Connections = rOldPIVRec.PIV_Connections + argData.PIV_Connections
                            PIV_TxBytes = rOldPIVRec.PIV_TxBytes + argData.PIV_TxBytes
                            PIV_RxBytes = rOldPIVRec.PIV_RxBytes + argData.PIV_RxBytes
                            PIV_LatencySum = rOldPIVRec.PIV_LatencySum + argData.PIV_LatencySum
                            PIV_LatencyCount = rOldPIVRec.PIV_LatencyCount + argData.PIV_LatencyCount
                            PIV_Jitter = if argData.PIV_LatencyCount > 0u then argData.PIV_Jitter else rOldPIVRec.PIV_Jitter
                            PIV_ReqCount = rOldPIVRec.PIV_ReqCount + argData.PIV_ReqCount
                            PIV_ResCount = rOldPIVRec.PIV_ResCount + argData.PIV_ResCount
                            PIV_DropCount = rOldPIVRec.PIV_DropCount + argData.PIV_DropCount
                        }
                    )
            )
            ()
   
    /// 測定値を取得する
    member this.Pop () =
        // 秒単位でみて現在時刻と一致するレコードを除き、
        // 保持している測定値をすべて返却する

        lock ( this.rPerformanceData ) ( fun _ ->
            // 現在時刻の検索キー
            let ct = System.DateTime.Now
            let sk =
                sprintf
                    "%04d%02d%02d%02d%02d%02d"
                    ct.Year ct.Month ct.Day ct.Hour ct.Minute ct.Second
            
            // 複製する
            let rRet = new SortedList( this.rPerformanceData )

            // 現在時刻のレコードが存在する場合は取り除く
            rRet.Remove sk

            // 現在時刻のレコードを検索する
            let rCurRec = this.rPerformanceData.Item sk

            // 一度すべてのレコードを削除して、現在時刻のレコードだけ入れなおす
            this.rPerformanceData.Clear ()
            if rCurRec <> null then
                this.rPerformanceData.Add( sk, rCurRec )

            rRet
        )


