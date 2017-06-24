///////////////////////////////////////////////////////////////////////////////
// ConfTcpServer.fs : TCPServer用詳細設定画面
// 

namespace ConfTcpServer

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open Microsoft.Win32

type CConfTcpServer( argConfig : Config.CConfig ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/ConfTcpServer.xaml", UriKind.Relative )
         ) :?> Window
        
    // コントールのオブジェクトを取得する
    let m_ReceiveOnlyCheck = m_Window.FindName( "ReceiveOnlyCheck" ) :?> CheckBox
    let m_DisableNagleCheck = m_Window.FindName( "DisableNagleCheck" ) :?> CheckBox
    let m_ReceiveBufferSizeTextBox = m_Window.FindName( "ReceiveBufferSizeTextBox" ) :?> TextBox
    let m_SendBufferSizeTextBox = m_Window.FindName( "SendBufferSizeTextBox" ) :?> TextBox
    let m_EnableTrafficShapingCheck = m_Window.FindName( "EnableTrafficShapingCheck" ) :?> CheckBox
    let m_MaxBytesPerSecTextBox = m_Window.FindName( "MaxBytesPerSecTextBox" ) :?> TextBox
    let m_MinBytesPerSecTextBox = m_Window.FindName( "MinBytesPerSecTextBox" ) :?> TextBox
    let m_WavelengthTextBox = m_Window.FindName( "WavelengthTextBox" ) :?> TextBox
    let m_PhaseTextBox = m_Window.FindName( "PhaseTextBox" ) :?> TextBox
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    //-------------------------------------------------------------------------
    // メソッド

    // ウインドウの初期化処理
    member this.Initialize argOwner =
        // イベントハンドラの追加
        m_ReceiveOnlyCheck.Click.AddHandler ( fun sender e -> this.ReceiveOnlyCheck_Click sender e )
        m_EnableTrafficShapingCheck.Click.AddHandler ( fun sender e -> this.EnableTrafficShapingCheck_Click sender e )
        m_OKButton.Click.AddHandler ( fun sender e -> this.OKButton_Click sender e )
        m_CancelButton.Click.AddHandler ( fun sender e -> this.CancelButton_Click sender e )

        // 初期値の設定
        m_ReceiveOnlyCheck.IsChecked <- Nullable rConfig.TCPServer_ReceiveOnly
        m_DisableNagleCheck.IsChecked <- Nullable rConfig.TCPServer_DisableNagle
        m_ReceiveBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPServer_ReceiveBufferSize
        m_SendBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPServer_SendBufferSize
        m_EnableTrafficShapingCheck.IsChecked <- Nullable rConfig.TCPServer_EnableTrafficShaping
        m_MinBytesPerSecTextBox.Text <- sprintf "%d" rConfig.TCPServer_MinBytesPerSec
        m_MaxBytesPerSecTextBox.Text <- sprintf "%d" rConfig.TCPServer_MaxBytesPerSec
        m_WavelengthTextBox.Text <- sprintf "%d" rConfig.TCPServer_Wavelength
        m_PhaseTextBox.Text <- sprintf "%d" rConfig.TCPServer_Phase

        // コントロールの有効・無効の設定
        this.SetEnableControl rConfig.TCPServer_ReceiveOnly rConfig.TCPServer_EnableTrafficShaping

        m_Window.Owner <- argOwner

    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )

    //-------------------------------------------------------------------------
    // プライベートメソッド

    // コントロールの有効/無効を変更する
    member private this.SetEnableControl argReceiveOnly argEnableTrafficShaping =
        // 受信専用であれば、帯域制御は関係ない
        m_EnableTrafficShapingCheck.IsEnabled <- not argReceiveOnly

        // 受信専用、ないし帯域制御を使用しない場合は、帯域制御関連のコントロールを無効化する
        m_MinBytesPerSecTextBox.IsEnabled <- ( not argReceiveOnly ) && argEnableTrafficShaping
        m_MaxBytesPerSecTextBox.IsEnabled <- ( not argReceiveOnly ) && argEnableTrafficShaping
        m_WavelengthTextBox.IsEnabled <- ( not argReceiveOnly ) && argEnableTrafficShaping
        m_PhaseTextBox.IsEnabled <- ( not argReceiveOnly ) && argEnableTrafficShaping

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // ReceiveOnlyチェックボックスの変化
    member private this.ReceiveOnlyCheck_Click sender e =
        this.SetEnableControl
            ( m_ReceiveOnlyCheck.IsChecked.GetValueOrDefault false )
            ( m_EnableTrafficShapingCheck.IsChecked.GetValueOrDefault false )

    // EnableTrafficShapingチェックボックスの変化
    member private this.EnableTrafficShapingCheck_Click sender e =
        this.SetEnableControl
            ( m_ReceiveOnlyCheck.IsChecked.GetValueOrDefault false )
            ( m_EnableTrafficShapingCheck.IsChecked.GetValueOrDefault false )

    // OKボタンの押下
    member private this.OKButton_Click sender e =
        // 入力値を取得する
        rConfig.TCPServer_ReceiveOnly <- m_ReceiveOnlyCheck.IsChecked.GetValueOrDefault true
        rConfig.TCPServer_DisableNagle <- m_DisableNagleCheck.IsChecked.GetValueOrDefault false

        rConfig.TCPServer_ReceiveBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_ReceiveBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.TCPServer_SendBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_SendBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.TCPServer_EnableTrafficShaping <- m_EnableTrafficShapingCheck.IsChecked.GetValueOrDefault true
        rConfig.TCPServer_MinBytesPerSec <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MinBytesPerSecTextBox.Text
                    0L
                    2147483647L
                    12500000L
                    ignore
            )
        rConfig.TCPServer_MaxBytesPerSec <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_MaxBytesPerSecTextBox.Text
                    ( int64 rConfig.TCPServer_MinBytesPerSec )
                    2147483647L
                    125000000L
                    ignore
            )
        rConfig.TCPServer_Wavelength <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_WavelengthTextBox.Text
                    1L
                    3600L
                    10L
                    ignore
            )
        rConfig.TCPServer_Phase <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_PhaseTextBox.Text
                    0L
                    3600L
                    0L
                    ignore
            )
        m_Window.Close ()

    // Cancelボタンの押下
    member private this.CancelButton_Click sender e =
        // 保存せずに閉じる
        m_Window.Close ()
