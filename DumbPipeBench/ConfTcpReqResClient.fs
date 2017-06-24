///////////////////////////////////////////////////////////////////////////////
// ConfTcpReqResClient.fs : TCPReqResClient用詳細設定画面
// 

namespace ConfTcpReqResClient

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open Microsoft.Win32

type CConfTcpReqResClient( argConfig : Config.CConfig ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/ConfTcpReqResClient.xaml", UriKind.Relative )
         ) :?> Window
        
    // コントールのオブジェクトを取得する
    let m_DisableNagleCheck = m_Window.FindName( "DisableNagleCheck" ) :?> CheckBox
    let m_ReceiveBufferSizeTextBox = m_Window.FindName( "ReceiveBufferSizeTextBox" ) :?> TextBox
    let m_SendBufferSizeTextBox = m_Window.FindName( "SendBufferSizeTextBox" ) :?> TextBox
    let m_ConnectionCountTextBox = m_Window.FindName( "ConnectionCountTextBox" ) :?> TextBox
    let m_RampUpTimeTextBox = m_Window.FindName( "RampUpTimeTextBox" ) :?> TextBox
    let m_MinReqestDataSizeTextBox = m_Window.FindName( "MinReqestDataSizeTextBox" ) :?> TextBox
    let m_MaxReqestDataSizeTextBox = m_Window.FindName( "MaxReqestDataSizeTextBox" ) :?> TextBox
    let m_MinResponceDataSizeTextBox = m_Window.FindName( "MinResponceDataSizeTextBox" ) :?> TextBox
    let m_MaxResponceDataSizeTextBox = m_Window.FindName( "MaxResponceDataSizeTextBox" ) :?> TextBox
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    //-------------------------------------------------------------------------
    // メソッド

    // ウインドウの初期化処理
    member this.Initialize argOwner =
        // イベントハンドラの追加
        m_OKButton.Click.AddHandler ( fun sender e -> this.OKButton_Click sender e )
        m_CancelButton.Click.AddHandler ( fun sender e -> this.CancelButton_Click sender e )

        // 初期値の設定
        m_DisableNagleCheck.IsChecked <- Nullable rConfig.TCPReqResClient_DisableNagle
        m_ReceiveBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_ReceiveBufferSize
        m_SendBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_SendBufferSize
        m_ConnectionCountTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_MaxConnectionCount
        m_RampUpTimeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_RampUpTime
        m_MinReqestDataSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_MinReqestDataLength
        m_MaxReqestDataSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_MaxReqestDataLength
        m_MinResponceDataSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_MinResponceDataLength
        m_MaxResponceDataSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResClient_MaxResponceDataLength

        m_Window.Owner <- argOwner

    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // OKボタンの押下
    member private this.OKButton_Click sender e =
        // 入力値を取得する
        rConfig.TCPReqResClient_DisableNagle <- m_DisableNagleCheck.IsChecked.GetValueOrDefault false

        rConfig.TCPReqResClient_ReceiveBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_ReceiveBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.TCPReqResClient_SendBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_SendBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.TCPReqResClient_MaxConnectionCount <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_ConnectionCountTextBox.Text
                    1L
                    255L
                    1L
                    ignore
            )
        rConfig.TCPReqResClient_RampUpTime <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_RampUpTimeTextBox.Text
                    0L
                    60L
                    1L
                    ignore
            )
        rConfig.TCPReqResClient_MinReqestDataLength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MinReqestDataSizeTextBox.Text
                    64L
                    65536L
                    64L
                    ignore
            )
        rConfig.TCPReqResClient_MaxReqestDataLength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MaxReqestDataSizeTextBox.Text
                    ( int64 rConfig.TCPReqResClient_MinReqestDataLength )
                    65536L
                    ( int64 rConfig.TCPReqResClient_MinResponceDataLength )
                    ignore
            )
        rConfig.TCPReqResClient_MinResponceDataLength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MinResponceDataSizeTextBox.Text
                    64L
                    65536L
                    64L
                    ignore
            )
        rConfig.TCPReqResClient_MaxResponceDataLength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MaxResponceDataSizeTextBox.Text
                    ( int64 rConfig.TCPReqResClient_MinResponceDataLength )
                    65536L
                    ( int64 rConfig.TCPReqResClient_MinResponceDataLength )
                    ignore
            )
        m_Window.Close ()

    // Cancelボタンの押下
    member private this.CancelButton_Click sender e =
        // 保存せずに閉じる
        m_Window.Close ()

