///////////////////////////////////////////////////////////////////////////////
// Constant.fs : アプリケーションで使用する定数の定義
// 

module Constant

open System
open System.Xml
open System.Xml.Linq

/// レジストリキーのルートノードの名称
[<Literal>]
let RegRootKey = @"Software\nabiki_t\DumpPipeBench"

/// 定義を保持するレジストリの値の名前
[<Literal>]
let ConfigRegValueName = "DefConfig"

/// XMLの各要素の名前
let XNAME_BumbPipeBenchConf = XName.Get( "BumbPipeBenchConf" )

let XNAME_Ver100 = XName.Get( "Ver100" )

let XNAME_InputParam = XName.Get( "InputParam" )
let XNAME_RunMode = XName.Get( "RunMode" )
let XNAME_TargetAddress = XName.Get( "TargetAddress" )
let XNAME_PortNumber = XName.Get( "PortNumber" )
let XNAME_LogFileName = XName.Get( "LogFileName" )
let XNAME_AutoScroll = XName.Get( "AutoScroll" )

let XNAME_TCPClientParam = XName.Get( "TCPClientParam" )
let XNAME_ReceiveOnly = XName.Get( "ReceiveOnly" )
let XNAME_DisableNagle = XName.Get( "DisableNagle" )
let XNAME_ReceiveBufferSize = XName.Get( "ReceiveBufferSize" )
let XNAME_SendBufferSize = XName.Get( "SendBufferSize" )
let XNAME_MaxConnectionCount = XName.Get( "MaxConnectionCount" )
let XNAME_RampUpTime = XName.Get( "RampUpTime" )
let XNAME_EnableTrafficShaping = XName.Get( "EnableTrafficShaping" )
let XNAME_MaxBytesPerSec = XName.Get( "MaxBytesPerSec" )
let XNAME_MinBytesPerSec = XName.Get( "MinBytesPerSec" )
let XNAME_Wavelength = XName.Get( "Wavelength" )
let XNAME_Phase = XName.Get( "Phase" )

let XNAME_TCPServerParam = XName.Get( "TCPServerParam" )

let XNAME_TCPReqResClientParam = XName.Get( "TCPReqResClientParam" )
let XNAME_MinReqestDataLength = XName.Get( "MinReqestDataLength" )
let XNAME_MaxReqestDataLength = XName.Get( "MaxReqestDataLength" )
let XNAME_MinResponceDataLength = XName.Get( "MinResponceDataLength" )
let XNAME_MaxResponceDataLength = XName.Get( "MaxResponceDataLength" )

let XNAME_TCPReqResServerParam = XName.Get( "TCPReqResServerParam" )

let XNAME_UDPClientParam = XName.Get( "UDPClientParam" )
let XNAME_DontFragment = XName.Get( "DontFragment" )
let XNAME_TTL = XName.Get( "TTL" )
let XNAME_DatagramSize = XName.Get( "DatagramSize" )


let XNAME_ScreenMetrix = XName.Get( "ScreenMetrix" )
let XNAME_ListColumnWidth00 = XName.Get( "ListColumnWidth00" )
let XNAME_ListColumnWidth01 = XName.Get( "ListColumnWidth01" )
let XNAME_ListColumnWidth02 = XName.Get( "ListColumnWidth02" )
let XNAME_ListColumnWidth03 = XName.Get( "ListColumnWidth03" )
let XNAME_ListColumnWidth04 = XName.Get( "ListColumnWidth04" )
let XNAME_ListColumnWidth05 = XName.Get( "ListColumnWidth05" )
let XNAME_ListColumnWidth06 = XName.Get( "ListColumnWidth06" )
let XNAME_ListColumnWidth07 = XName.Get( "ListColumnWidth07" )


/// 実行時のモードを示す定数
type RunMode =
    | TCPClient
    | TCPServer
    | TCPReqResClient
    | TCPReqResServer
    | UDPClient

// 例外の定義
exception NegotiationError of string
exception DataReceiveError of string

