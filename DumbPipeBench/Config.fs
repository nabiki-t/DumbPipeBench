///////////////////////////////////////////////////////////////////////////////
// Config.fs : CConfigクラスの実装
// 定義を保持・保存する

namespace Config

open System
open System.Xml
open System.Xml.Linq
open System.Xml.Schema
open Microsoft.Win32
open Constant

// 定義を保持するクラス
type CConfig =
    //-------------------------------------------------------------------------
    // メンバ変数

    // 定義を保持する
    // 面倒だからXMLのオブジェクトそのままで保持しておくものとする
    val xdoc : XDocument

    //-------------------------------------------------------------------------
    // 構築・破棄

    // 引数なし。レジストリからデフォルト値をロードする。
    public new() = 
        { xdoc = CConfig.LoadRegistory () }

    /// CConfig指定のコンストラクタ。オブジェクトを複製する。
    public new( r : CConfig ) =
        { xdoc = new XDocument( r.xdoc ) }

    //-------------------------------------------------------------------------
    // メソッド

     /// レジストリへ保存する
    member public this.SaveRegistory () =
        // HKEY_CURRENT_USER配下にキーを作る
        use key = Registry.CurrentUser.CreateSubKey( RegRootKey )
        // XMLの定義をそのまま文字列化して、レジストリに保存する
        key.SetValue( ConfigRegValueName, this.xdoc.ToString () ) |> ignore

    // 動作モードのプロパティ
    member public this.RunMode
        with get() =
            match ( CConfig.GetInputParamChild this.xdoc XNAME_RunMode ).Value with
            | "TCPClient" -> TCPClient
            | "TCPServer" -> TCPServer
            | "TCPReqResClient" -> TCPReqResClient
            | "TCPReqResServer" -> TCPReqResServer
            | "UDPClient" -> UDPClient
            | _ -> TCPClient 
        and set v =
            ( CConfig.GetInputParamChild this.xdoc XNAME_RunMode ).Value <-
                match v with
                | TCPClient -> "TCPClient"
                | TCPServer -> "TCPServer"
                | TCPReqResClient -> "TCPReqResClient"
                | TCPReqResServer -> "TCPReqResServer"
                | UDPClient -> "UDPClient"

    // ターゲットのアドレスのプロパティ
    member public this.TargetAddress
        with get() =
            ( CConfig.GetInputParamChild this.xdoc XNAME_TargetAddress ).Value
        and set v =
            ( CConfig.GetInputParamChild this.xdoc XNAME_TargetAddress ).Value <- v

    // ポート番号のプロパティ
    member public this.PortNumber
        with get() =
            uint16 ( CConfig.GetInputParamChild this.xdoc XNAME_PortNumber ).Value
        and set ( v : uint16 ) =
            ( CConfig.GetInputParamChild this.xdoc XNAME_PortNumber ).Value <- sprintf "%u" v

    // ログファイル名のプロパティ
    member public this.LogFileName
        with get() =
            ( CConfig.GetInputParamChild this.xdoc XNAME_LogFileName ).Value
        and set v =
            ( CConfig.GetInputParamChild this.xdoc XNAME_LogFileName ).Value <- v

    // 自動スクロールのプロパティ
    member public this.AutoScroll
        with get() =
            String.Compare( ( CConfig.GetInputParamChild this.xdoc XNAME_AutoScroll ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetInputParamChild this.xdoc XNAME_AutoScroll ).Value <- if v then "True" else "False"

    // TCPClientのReceiveOnlyのプロパティ
    member public this.TCPClient_ReceiveOnly
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_ReceiveOnly ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_ReceiveOnly ).Value <- if v then "True" else "False"

    // TCPClientのDisableNagleのプロパティ
    member public this.TCPClient_DisableNagle
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_DisableNagle ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_DisableNagle ).Value <- if v then "True" else "False"

    // TCPClientのReceiveBufferSizeのプロパティ
    member public this.TCPClient_ReceiveBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_ReceiveBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_ReceiveBufferSize ).Value <- sprintf "%u" v

    // TCPClientのSendBufferSizeのプロパティ
    member public this.TCPClient_SendBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_SendBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_SendBufferSize ).Value <- sprintf "%u" v

    // TCPClientの最大コネクション数のプロパティ
    member public this.TCPClient_MaxConnectionCount
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MaxConnectionCount ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MaxConnectionCount ).Value <- sprintf "%u" v

    // TCPClientのランプアップ時間のプロパティ
    member public this.TCPClient_RampUpTime
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_RampUpTime ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_RampUpTime ).Value <- sprintf "%u" v

    // TCPClientの帯域制御有効化有無のプロパティ
    member public this.TCPClient_EnableTrafficShaping
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_EnableTrafficShaping ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_EnableTrafficShaping ).Value <- if v then "True" else "False"

    // TCPClientの最小帯域幅のプロパティ
    member public this.TCPClient_MinBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MinBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MinBytesPerSec ).Value <- sprintf "%u" v

    // TCPClientの最大帯域幅のプロパティ
    member public this.TCPClient_MaxBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MaxBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_MaxBytesPerSec ).Value <- sprintf "%u" v

    // TCPClientの波長のプロパティ
    member public this.TCPClient_Wavelength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_Wavelength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_Wavelength ).Value <- sprintf "%u" v

    // TCPClientの位相のプロパティ
    member public this.TCPClient_Phase
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_Phase ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPClientParam XNAME_Phase ).Value <- sprintf "%u" v

    // TCPServerのReceiveOnlyのプロパティ
    member public this.TCPServer_ReceiveOnly
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_ReceiveOnly ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_ReceiveOnly ).Value <- if v then "True" else "False"

    // TCPServerのDisableNagleのプロパティ
    member public this.TCPServer_DisableNagle
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_DisableNagle ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_DisableNagle ).Value <- if v then "True" else "False"

    // TCPServerのReceiveBufferSizeのプロパティ
    member public this.TCPServer_ReceiveBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_ReceiveBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_ReceiveBufferSize ).Value <- sprintf "%u" v

    // TCPServerのSendBufferSizeのプロパティ
    member public this.TCPServer_SendBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_SendBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_SendBufferSize ).Value <- sprintf "%u" v

    // TCPServerの帯域制御有効化有無のプロパティ
    member public this.TCPServer_EnableTrafficShaping
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_EnableTrafficShaping ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_EnableTrafficShaping ).Value <- if v then "True" else "False"

    // TCPServerの最小帯域幅のプロパティ
    member public this.TCPServer_MinBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_MinBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_MinBytesPerSec ).Value <- sprintf "%u" v

    // TCPServerの最大帯域幅のプロパティ
    member public this.TCPServer_MaxBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_MaxBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_MaxBytesPerSec ).Value <- sprintf "%u" v

    // TCPServerの波長のプロパティ
    member public this.TCPServer_Wavelength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_Wavelength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_Wavelength ).Value <- sprintf "%u" v

    // TCPServerの位相のプロパティ
    member public this.TCPServer_Phase
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_Phase ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPServerParam XNAME_Phase ).Value <- sprintf "%u" v

    // TCPReqResClientのDisableNagleのプロパティ
    member public this.TCPReqResClient_DisableNagle
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_DisableNagle ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_DisableNagle ).Value <- if v then "True" else "False"

    // TCPReqResClientのReceiveBufferSizeのプロパティ
    member public this.TCPReqResClient_ReceiveBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_ReceiveBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_ReceiveBufferSize ).Value <- sprintf "%u" v

    // TCPReqResClientのSendBufferSizeのプロパティ
    member public this.TCPReqResClient_SendBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_SendBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_SendBufferSize ).Value <- sprintf "%u" v

    // TCPReqResClientの最大コネクション数のプロパティ
    member public this.TCPReqResClient_MaxConnectionCount
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxConnectionCount ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxConnectionCount ).Value <- sprintf "%u" v

    // TCPReqResClientのランプアップ時間のプロパティ
    member public this.TCPReqResClient_RampUpTime
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_RampUpTime ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_RampUpTime ).Value <- sprintf "%u" v

    // TCPReqResClientの最小要求データ長のプロパティ
    member public this.TCPReqResClient_MinReqestDataLength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MinReqestDataLength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MinReqestDataLength ).Value <- sprintf "%u" v

    // TCPReqResClientの最大要求データ長のプロパティ
    member public this.TCPReqResClient_MaxReqestDataLength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxReqestDataLength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxReqestDataLength ).Value <- sprintf "%u" v

    // TCPReqResClientの最小応答データ長のプロパティ
    member public this.TCPReqResClient_MinResponceDataLength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MinResponceDataLength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MinResponceDataLength ).Value <- sprintf "%u" v

    // TCPReqResClientの最大応答データ長のプロパティ
    member public this.TCPReqResClient_MaxResponceDataLength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxResponceDataLength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResClientParam XNAME_MaxResponceDataLength ).Value <- sprintf "%u" v

    // TCPReqResServerのDisableNagleのプロパティ
    member public this.TCPReqResServer_DisableNagle
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_DisableNagle ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_DisableNagle ).Value <- if v then "True" else "False"

    // TCPReqResServerのReceiveBufferSizeのプロパティ
    member public this.TCPReqResServer_ReceiveBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_ReceiveBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_ReceiveBufferSize ).Value <- sprintf "%u" v

    // TCPReqResServerのSendBufferSizeのプロパティ
    member public this.TCPReqResServer_SendBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_SendBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_TCPReqResServerParam XNAME_SendBufferSize ).Value <- sprintf "%u" v

    // UDPClientのReceiveOnlyのプロパティ
    member public this.UDPClient_ReceiveOnly
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_ReceiveOnly ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_ReceiveOnly ).Value <- if v then "True" else "False"

    // UDPClientのDontFragmentのプロパティ
    member public this.UDPClient_DontFragment
        with get() =
            String.Compare( ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_DontFragment ).Value, "True", true ) = 0
        and set v =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_DontFragment ).Value <- if v then "True" else "False"

    // UDPClientのTTLのプロパティ
    member public this.UDPClient_TTL
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_TTL ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_TTL ).Value <- sprintf "%u" v

    // UDPClientのReceiveBufferSizeのプロパティ
    member public this.UDPClient_ReceiveBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_ReceiveBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_ReceiveBufferSize ).Value <- sprintf "%u" v

    // UDPClientのSendBufferSizeのプロパティ
    member public this.UDPClient_SendBufferSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_SendBufferSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_SendBufferSize ).Value <- sprintf "%u" v

    // UDPClientのMinBytesPerSecのプロパティ
    member public this.UDPClient_MinBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_MinBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_MinBytesPerSec ).Value <- sprintf "%u" v

    // UDPClientのMaxBytesPerSecのプロパティ
    member public this.UDPClient_MaxBytesPerSec
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_MaxBytesPerSec ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_MaxBytesPerSec ).Value <- sprintf "%u" v

    // UDPClientのWavelengthのプロパティ
    member public this.UDPClient_Wavelength
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_Wavelength ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_Wavelength ).Value <- sprintf "%u" v

    // UDPClientのPhaseのプロパティ
    member public this.UDPClient_Phase
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_Phase ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_Phase ).Value <- sprintf "%u" v

    // UDPClientのDatagramSizeのプロパティ
    member public this.UDPClient_DatagramSize
        with get() =
            uint32 ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_DatagramSize ).Value
        and set ( v : uint32 ) =
            ( CConfig.GetConfItem this.xdoc XNAME_UDPClientParam XNAME_DatagramSize ).Value <- sprintf "%u" v

    // リストのカラム幅のプロパティ
    member public this.ListColumnWidth
        with get ( idx : int ) =
            match idx with
            | 0 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth00 ).Value
            | 1 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth01 ).Value
            | 2 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth02 ).Value
            | 3 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth03 ).Value
            | 4 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth04 ).Value
            | 5 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth05 ).Value
            | 6 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth06 ).Value
            | 7 -> float ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth07 ).Value
            | _ -> 100.0
        and set ( idx : int ) ( v : float ) =
            match idx with
            | 0 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth00 ).Value <- sprintf "%u" ( int v )
            | 1 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth01 ).Value <- sprintf "%u" ( int v )
            | 2 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth02 ).Value <- sprintf "%u" ( int v )
            | 3 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth03 ).Value <- sprintf "%u" ( int v )
            | 4 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth04 ).Value <- sprintf "%u" ( int v )
            | 5 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth05 ).Value <- sprintf "%u" ( int v )
            | 6 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth06 ).Value <- sprintf "%u" ( int v )
            | 7 -> ( CConfig.GetScreenMetrixChild this.xdoc XNAME_ListColumnWidth07 ).Value <- sprintf "%u" ( int v )
            | _ -> ()

    //-------------------------------------------------------------------------
    // プライベートメソッド

    /// レジストリから定義を読み込む
    static member private LoadRegistory () =
        try
            // レジストリキーを開く
            use key = Registry.CurrentUser.CreateSubKey RegRootKey

            // XMLの定義を保持する値を取得する
            key.GetValue ConfigRegValueName :?> string
                |> XDocument.Parse
                |> CConfig.Validate 

        with
        | _ as e ->
            // 何らかの事情により定義がロードできなければ、
            // 黙って初期値を設定する
            CConfig.CreateInitiConf

    /// 取得したXMLが正しいか否か検証する
    static member private Validate argDoc =

        // XSDをリソースから取得する
        use xsdStream = Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream( "Config.xsd" )
        let confSchamaSet =
            use xsdReader = XmlReader.Create xsdStream
            let wSS = new XmlSchemaSet ()
            wSS.Add( null, xsdReader ) |> ignore
            wSS

        // 取得した定義が正しいか確認する
        argDoc.Validate(
            confSchamaSet,
            fun argObj argEx ->
                ()
        )

        // 正しければ引数をそのまま返す
        argDoc

    // 初期状態の定義を作る
    static member private CreateInitiConf =
        new XDocument(
            new XElement(
                XNAME_BumbPipeBenchConf,
                new XElement(
                    XNAME_Ver100,
                    new XElement(
                        XNAME_InputParam,
                        new XElement( XNAME_RunMode, "TCPClient" ),
                        new XElement( XNAME_TargetAddress, "" ),
                        new XElement( XNAME_PortNumber, 60000 ),
                        new XElement( XNAME_LogFileName, "" ),
                        new XElement( XNAME_AutoScroll, "True" )
                    ),
                    new XElement(
                        XNAME_TCPClientParam,
                        new XElement( XNAME_ReceiveOnly, "False" ),
                        new XElement( XNAME_DisableNagle, "False" ),
                        new XElement( XNAME_ReceiveBufferSize, 8192 ),
                        new XElement( XNAME_SendBufferSize, 8192 ),
                        new XElement( XNAME_MaxConnectionCount, 1 ),
                        new XElement( XNAME_RampUpTime, 1 ),
                        new XElement( XNAME_EnableTrafficShaping, "False" ),
                        new XElement( XNAME_MinBytesPerSec, 12500000 ),
                        new XElement( XNAME_MaxBytesPerSec, 125000000 ),
                        new XElement( XNAME_Wavelength, 10 ),
                        new XElement( XNAME_Phase, 0 )
                    ),
                    new XElement(
                        XNAME_TCPServerParam,
                        new XElement( XNAME_ReceiveOnly, "False" ),
                        new XElement( XNAME_DisableNagle, "False" ),
                        new XElement( XNAME_ReceiveBufferSize, 8192 ),
                        new XElement( XNAME_SendBufferSize, 8192 ),
                        new XElement( XNAME_EnableTrafficShaping, "False" ),
                        new XElement( XNAME_MinBytesPerSec, 12500000 ),
                        new XElement( XNAME_MaxBytesPerSec, 125000000 ),
                        new XElement( XNAME_Wavelength, 10 ),
                        new XElement( XNAME_Phase, 0 )
                    ),
                    new XElement(
                        XNAME_TCPReqResClientParam,
                        new XElement( XNAME_DisableNagle, "False" ),
                        new XElement( XNAME_ReceiveBufferSize, 8192 ),
                        new XElement( XNAME_SendBufferSize, 8192 ),
                        new XElement( XNAME_MaxConnectionCount, 1 ),
                        new XElement( XNAME_RampUpTime, 1 ),
                        new XElement( XNAME_MinReqestDataLength, 64 ),
                        new XElement( XNAME_MaxReqestDataLength, 64 ),
                        new XElement( XNAME_MinResponceDataLength, 64 ),
                        new XElement( XNAME_MaxResponceDataLength, 64 )
                    ),
                    new XElement(
                        XNAME_TCPReqResServerParam,
                        new XElement( XNAME_DisableNagle, "False" ),
                        new XElement( XNAME_ReceiveBufferSize, 8192 ),
                        new XElement( XNAME_SendBufferSize, 8192 )
                    ),
                    new XElement(
                        XNAME_UDPClientParam,
                        new XElement( XNAME_ReceiveOnly, "False" ),
                        new XElement( XNAME_DontFragment, "True" ),
                        new XElement( XNAME_TTL, 32 ),
                        new XElement( XNAME_ReceiveBufferSize, 8192 ),
                        new XElement( XNAME_SendBufferSize, 8192 ),
                        new XElement( XNAME_MinBytesPerSec, 125000 ),
                        new XElement( XNAME_MaxBytesPerSec, 256000 ),
                        new XElement( XNAME_Wavelength, 10 ),
                        new XElement( XNAME_Phase, 0 ),
                        new XElement( XNAME_DatagramSize, 1024 )
                    ),
                    new XElement(
                        XNAME_ScreenMetrix,
                        new XElement( XNAME_ListColumnWidth00, 30 ),
                        new XElement( XNAME_ListColumnWidth01, 120 ),
                        new XElement( XNAME_ListColumnWidth02, 100 ),
                        new XElement( XNAME_ListColumnWidth03, 100 ),
                        new XElement( XNAME_ListColumnWidth04, 100 ),
                        new XElement( XNAME_ListColumnWidth05, 100 ),
                        new XElement( XNAME_ListColumnWidth06, 100 ),
                        new XElement( XNAME_ListColumnWidth07, 100 )
                    )
                )
            )
        )

    // 指定した種別の配下にあるXElementを取得する
    static member private GetConfItem argDoc argP argC : XElement =
        argDoc.Element( XNAME_BumbPipeBenchConf ).
            Element( XNAME_Ver100 ).
                Element( argP ).
                    Element( argC )
   
    // InputParam配下の指定されたXElementを取得する
    static member private GetInputParamChild argDoc name : XElement =
        CConfig.GetConfItem argDoc XNAME_InputParam name

    // ScreenMetrix配下の指定されたXElementを取得する
    static member private GetScreenMetrixChild argDoc name : XElement =
        CConfig.GetConfItem argDoc XNAME_ScreenMetrix name
