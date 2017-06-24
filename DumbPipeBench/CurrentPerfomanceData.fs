///////////////////////////////////////////////////////////////////////////////
// CCurrentPerfomanceData.fs : CCCurrentPerfomanceDataの実装
//

namespace CurrentPerfomanceData

open System
open System.Windows.Data
open System.Collections.ObjectModel

/// 測定値1件分（値）を保持するレコード
type PerfomItemVal =
    {
        PIV_ScanCount : uint32      // 測定回数
        PIV_Time : DateTime         // 時刻
        PIV_Connections : uint32    // コネクション数
        PIV_TxBytes : uint64        // 秒間送信バイト数
        PIV_RxBytes : uint64        // 秒間受信バイト数
        PIV_LatencySum : double     // 送信遅延時間（合計）
        PIV_LatencyCount : uint32   // 送信遅延時間測定回数
        PIV_Jitter : double         // ジッタ
        PIV_ReqCount : uint32       // 要求（送信）回数
        PIV_ResCount : uint32       // 応答（受信）回数
        PIV_DropCount : uint32       // エラー発生回数
    }

/// 測定値1件分（編集済み文字列）を保持するレコード
type PerfomItemStr =
    {
        Number : string             // 項番
        Time : string               // 時刻
        Connections : string        // コネクション数
        Connections_RV : float      // コネクション数（数値）     
        TxBytes : string            // 秒間送信バイト数
        TxBytes_RV : float          // 秒間送信バイト数（数値）
        RxBytes : string            // 秒間受信バイト数
        RxBytes_RV : float          // 秒間受信バイト数（数値）
        Ext00 : string              // 拡張00
        Ext00_RV : float            // 拡張00（数値）
        Ext01 : string              // 拡張01
        Ext01_RV : float            // 拡張01（数値）
        Ext02 : string              // 拡張02
        Ext02_RV : float            // 拡張02（数値）
        Ext03 : string              // 拡張03
        Ext03_RV : float            // 拡張04（数値）
    }

/// 測定値を保持するクラス
type CCCurrentPerfomanceData =

    //-------------------------------------------------------------------------
    // メンバ変数

    // リストビューに表示するためのコレクション
    val m_DataItems : ObservableCollection< PerfomItemStr >

    // 表示する項目名（結果リスト、ログファイルのヘッダ、グラフ表示で使用する）
    val m_ColumnTitle : string[]

    // グラフ表示用の、項目の単位名
    val m_ColumnUnitStr : string[]

    //-------------------------------------------------------------------------
    // コンストラクタ

    new() =
        {
            m_DataItems = new ObservableCollection< PerfomItemStr >()
            m_ColumnTitle = [| ""; ""; ""; ""; ""; ""; ""; ""; "" |]
            m_ColumnUnitStr = [| ""; ""; ""; ""; ""; ""; ""; ""; "" |]
        }
    
    //-------------------------------------------------------------------------
    // 公開メソッド

    /// データを0件にする
    member this.Clear () =
        this.m_DataItems.Clear()

    /// 測定値を収集する
    member this.Add ( argLogFName : string ) argVal runMode =
        // リストに追加する
        let strRec = this.EditPerfomVal ( this.m_DataItems.Count + 1 ) argVal runMode
        this.m_DataItems.Add strRec

        // ログファイル名が指定されている場合は、ファイルに出力する
        try
            if argLogFName.Length > 0 then
                System.IO.File.AppendAllText(
                    argLogFName,
                    sprintf
                        "%s,%s,%s,%d,%d,%s,%s,%s,%s%s"
                        strRec.Number
                        strRec.Time
                        strRec.Connections
                        argVal.PIV_TxBytes
                        argVal.PIV_RxBytes
                        strRec.Ext00
                        strRec.Ext01
                        strRec.Ext02
                        strRec.Ext03
                        Environment.NewLine
                )
        with
        | _ as e ->
            // ログ出力は失敗しても無視する
            ()

    //-------------------------------------------------------------------------
    // プロパティ

    /// コレクションを取得
    member this.Items with get() = this.m_DataItems

    // 項目ごとの項目名
    member this.ColumnTitle
        with get idx =
            this.m_ColumnTitle.[idx]
        and set idx v =
            this.m_ColumnTitle.[idx] <- v

    // 項目ごとの単位
    member this.ColumnUnitStr
        with get idx =
            this.m_ColumnUnitStr.[idx]
        and set idx v =
            this.m_ColumnUnitStr.[idx] <- v

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// 測定値（値）を編集する
    member private thid.EditPerfomVal argNum argVal runMode =
        // 共通項目を先に算出しておく
        let wNumber = sprintf "%d" argNum
        let wTime = argVal.PIV_Time.ToLocalTime().ToString()
        let wConnections_RV = floor( float( argVal.PIV_Connections / argVal.PIV_ScanCount ) )
        let wConnections = sprintf "%d" ( int wConnections_RV )
        let wTxBytes_RV = float argVal.PIV_TxBytes
        let wTxBytes = String.Format( "{0:#,0}", argVal.PIV_TxBytes )
        let wRxBytes_RV = float argVal.PIV_RxBytes
        let wRxBytes = String.Format( "{0:#,0}", argVal.PIV_RxBytes )

        // 拡張項目を設定する
        match runMode with
        | Constant.TCPClient ->
            {
                Number = wNumber
                Time = wTime
                Connections = wConnections
                Connections_RV = wConnections_RV
                TxBytes = wTxBytes
                TxBytes_RV = wTxBytes_RV
                RxBytes = wRxBytes
                RxBytes_RV = wRxBytes_RV
                Ext00 = ""      // 使用しない
                Ext00_RV = 0.0
                Ext01 = ""      // 使用しない
                Ext01_RV = 0.0
                Ext02 = ""      // 使用しない
                Ext02_RV = 0.0
                Ext03 = ""      // 使用しない
                Ext03_RV = 0.0
            }
        | Constant.TCPServer ->
            {
                Number = wNumber
                Time = wTime
                Connections = wConnections
                Connections_RV = wConnections_RV
                TxBytes = wTxBytes
                TxBytes_RV = wTxBytes_RV
                RxBytes = wRxBytes
                RxBytes_RV = wRxBytes_RV
                Ext00 = ""      // 使用しない
                Ext00_RV = 0.0
                Ext01 = ""      // 使用しない
                Ext01_RV = 0.0
                Ext02 = ""      // 使用しない
                Ext02_RV = 0.0
                Ext03 = ""      // 使用しない
                Ext03_RV = 0.0
            }
        | Constant.TCPReqResClient ->
            {
                Number = wNumber
                Time = wTime
                Connections = wConnections
                Connections_RV = wConnections_RV
                TxBytes = wTxBytes
                TxBytes_RV = wTxBytes_RV
                RxBytes = wRxBytes
                RxBytes_RV = wRxBytes_RV

                // RTT(avg. ms)
                // 要求-応答を繰り返した時間と回数をもとに平均値を算出する
                Ext00 =
                    if argVal.PIV_LatencyCount > 0u then
                        sprintf "%0.3f" ( argVal.PIV_LatencySum / double argVal.PIV_LatencyCount )
                    else
                        "-"
                Ext00_RV =
                    if argVal.PIV_LatencyCount > 0u then
                        argVal.PIV_LatencySum / double argVal.PIV_LatencyCount
                    else
                        0.0

                // RTT Jitter(ms)
                // 最新値を出力するが、遅延時間が取得できていない場合は、
                // まっとうに計算されていないものと判断する
                Ext01 =
                   if argVal.PIV_LatencyCount > 0u then
                        sprintf "%0.3f" argVal.PIV_Jitter
                   else
                        "-"
                Ext01_RV =
                    if argVal.PIV_LatencyCount > 0u then argVal.PIV_Jitter else 0.0

                // Req-Res Count
                Ext02 = String.Format( "{0:#,0}", argVal.PIV_ReqCount )    
                Ext02_RV = float argVal.PIV_ReqCount

                Ext03 = ""      // 使用しない
                Ext03_RV = 0.0
            }
        | Constant.TCPReqResServer ->
            {
                Number = wNumber
                Time = wTime
                Connections = wConnections
                Connections_RV = wConnections_RV
                TxBytes = wTxBytes
                TxBytes_RV = wTxBytes_RV
                RxBytes = wRxBytes
                RxBytes_RV = wRxBytes_RV

                // Req-Res Count
                // 要求-応答を繰り返した時間と回数をもとに平均値を算出する
                Ext00 = String.Format( "{0:#,0}", argVal.PIV_ReqCount )
                Ext00_RV = float argVal.PIV_ReqCount

                Ext01 = ""      // 使用しない
                Ext01_RV = 0.0
                Ext02 = ""      // 使用しない
                Ext02_RV = 0.0
                Ext03 = ""      // 使用しない
                Ext03_RV = 0.0
            }
        | Constant.UDPClient ->
            {
                Number = wNumber
                Time = wTime
                Connections = wConnections
                Connections_RV = wConnections_RV
                TxBytes = wTxBytes
                TxBytes_RV = wTxBytes_RV
                RxBytes = wRxBytes
                RxBytes_RV = wRxBytes_RV

                // TxPackets/s
                Ext00 = String.Format( "{0:#,0}", argVal.PIV_ReqCount )
                Ext00_RV = float argVal.PIV_ReqCount

                // RxPackets/s
                Ext01 = String.Format( "{0:#,0}", argVal.PIV_ResCount )
                Ext01_RV = float argVal.PIV_ResCount

                // Jitter(ms)
                // 最新値を出力するが、遅延時間が取得できていない場合は、
                // まっとうに計算されていないものと判断する
                Ext02 =
                   if argVal.PIV_ResCount > 0u then
                        sprintf "%0.3f" argVal.PIV_Jitter
                   else
                        "-"
                Ext02_RV =
                    if argVal.PIV_ResCount > 0u then argVal.PIV_Jitter else 0.0

                // DropPackets/s
                Ext03 = String.Format( "{0:#,0}", argVal.PIV_DropCount )
                Ext03_RV = float argVal.PIV_DropCount

            }


    


