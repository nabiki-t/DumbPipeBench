
///////////////////////////////////////////////////////////////////////////////
// ConfUdpClient.fs : UDPClient用詳細設定画面
// 

namespace ConfUdpClient

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open Microsoft.Win32

type CConfUdpClient( argConfig : Config.CConfig ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/ConfUdpClient.xaml", UriKind.Relative )
         ) :?> Window
        
    // コントールのオブジェクトを取得する
    let m_ReceiveOnlyCheck = m_Window.FindName( "ReceiveOnlyCheck" ) :?> CheckBox
    let m_DontFragmentCheck = m_Window.FindName( "DontFragmentCheck" ) :?> CheckBox
    let m_TTLTextBox = m_Window.FindName( "TTLTextBox" ) :?> TextBox
    let m_ReceiveBufferSizeTextBox = m_Window.FindName( "ReceiveBufferSizeTextBox" ) :?> TextBox
    let m_SendBufferSizeTextBox = m_Window.FindName( "SendBufferSizeTextBox" ) :?> TextBox
    let m_MinBytesPerSecTextBox = m_Window.FindName( "MinBytesPerSecTextBox" ) :?> TextBox
    let m_MaxBytesPerSecTextBox = m_Window.FindName( "MaxBytesPerSecTextBox" ) :?> TextBox
    let m_WavelengthTextBox = m_Window.FindName( "WavelengthTextBox" ) :?> TextBox
    let m_PhaseTextBox = m_Window.FindName( "PhaseTextBox" ) :?> TextBox
    let m_DatagramSizeTextBox = m_Window.FindName( "DatagramSizeTextBox" ) :?> TextBox
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    //-------------------------------------------------------------------------
    // メソッド

    // ウインドウの初期化処理
    member this.Initialize argOwner =
        // イベントハンドラの追加
        m_ReceiveOnlyCheck.Click.AddHandler ( fun sender e -> this.ReceiveOnlyCheck_Click sender e )
        m_OKButton.Click.AddHandler ( fun sender e -> this.OKButton_Click sender e )
        m_CancelButton.Click.AddHandler ( fun sender e -> this.CancelButton_Click sender e )

        // 初期値の設定
        m_ReceiveOnlyCheck.IsChecked <- Nullable rConfig.UDPClient_ReceiveOnly
        m_DontFragmentCheck.IsChecked <- Nullable rConfig.UDPClient_DontFragment
        m_TTLTextBox.Text <- sprintf "%d" rConfig.UDPClient_TTL
        m_ReceiveBufferSizeTextBox.Text <- sprintf "%d" rConfig.UDPClient_ReceiveBufferSize
        m_SendBufferSizeTextBox.Text <- sprintf "%d" rConfig.UDPClient_SendBufferSize
        m_MinBytesPerSecTextBox.Text <- sprintf "%d" rConfig.UDPClient_MinBytesPerSec
        m_MaxBytesPerSecTextBox.Text <- sprintf "%d" rConfig.UDPClient_MaxBytesPerSec
        m_WavelengthTextBox.Text <- sprintf "%d" rConfig.UDPClient_Wavelength
        m_PhaseTextBox.Text <- sprintf "%d" rConfig.UDPClient_Phase
        m_DatagramSizeTextBox.Text <- sprintf "%d" rConfig.UDPClient_DatagramSize

        // コントロールの有効・無効の設定
        this.SetEnableControl rConfig.UDPClient_ReceiveOnly

        m_Window.Owner <- argOwner

    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )

    //-------------------------------------------------------------------------
    // プライベートメソッド

    // コントロールの有効/無効を変更する
    member private this.SetEnableControl argReceiveOnly =
        // 受信専用であれば、それ以外の送信にかかわるパラメタは関係ない

        m_DontFragmentCheck.IsEnabled <- ( not argReceiveOnly )
        m_TTLTextBox.IsEnabled <- ( not argReceiveOnly )
        m_MinBytesPerSecTextBox.IsEnabled <- ( not argReceiveOnly )
        m_MaxBytesPerSecTextBox.IsEnabled <- ( not argReceiveOnly )
        m_WavelengthTextBox.IsEnabled <- ( not argReceiveOnly )
        m_PhaseTextBox.IsEnabled <- ( not argReceiveOnly )
        m_DatagramSizeTextBox.IsEnabled <- ( not argReceiveOnly )

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // ReceiveOnlyチェックボックスの変化
    member private this.ReceiveOnlyCheck_Click sender e =
        this.SetEnableControl
            ( m_ReceiveOnlyCheck.IsChecked.GetValueOrDefault false )

    // OKボタンの押下
    member private this.OKButton_Click sender e =
        // 入力値を取得する
        rConfig.UDPClient_ReceiveOnly <- m_ReceiveOnlyCheck.IsChecked.GetValueOrDefault true
        rConfig.UDPClient_DontFragment <- m_DontFragmentCheck.IsChecked.GetValueOrDefault true

        rConfig.UDPClient_TTL <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_TTLTextBox.Text
                    0L
                    255L
                    32L
                    ignore
            )
        rConfig.UDPClient_ReceiveBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_ReceiveBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.UDPClient_SendBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_SendBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.UDPClient_MinBytesPerSec <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MinBytesPerSecTextBox.Text
                    0L
                    2147483647L
                    125000L
                    ignore
            )
        rConfig.UDPClient_MaxBytesPerSec <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MaxBytesPerSecTextBox.Text
                    ( int64 rConfig.UDPClient_MinBytesPerSec )
                    2147483647L
                    256000L
                    ignore
            )
        rConfig.UDPClient_Wavelength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_WavelengthTextBox.Text
                    1L
                    3600L
                    10L
                    ignore
            )
        rConfig.UDPClient_Phase <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_PhaseTextBox.Text
                    0L
                    3600L
                    0L
                    ignore
            )
        rConfig.UDPClient_DatagramSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_DatagramSizeTextBox.Text
                    64L
                    65536L
                    0L
                    ignore
            )
        m_Window.Close ()


    // Cancelボタンの押下
    member private this.CancelButton_Click sender e =
        // 保存せずに閉じる
        m_Window.Close ()

