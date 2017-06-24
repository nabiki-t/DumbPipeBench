///////////////////////////////////////////////////////////////////////////////
// ConfTcpReqResServer.fs : TCPReqResServer用詳細設定画面
// 

namespace ConfTcpReqResServer

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open Microsoft.Win32

type CConfTcpReqResServer( argConfig : Config.CConfig ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/ConfTcpReqResServer.xaml", UriKind.Relative )
         ) :?> Window
        
    // コントールのオブジェクトを取得する
    let m_DisableNagleCheck = m_Window.FindName( "DisableNagleCheck" ) :?> CheckBox
    let m_ReceiveBufferSizeTextBox = m_Window.FindName( "ReceiveBufferSizeTextBox" ) :?> TextBox
    let m_SendBufferSizeTextBox = m_Window.FindName( "SendBufferSizeTextBox" ) :?> TextBox
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
        m_DisableNagleCheck.IsChecked <- Nullable rConfig.TCPReqResServer_DisableNagle
        m_ReceiveBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResServer_ReceiveBufferSize
        m_SendBufferSizeTextBox.Text <- sprintf "%d" rConfig.TCPReqResServer_SendBufferSize

        m_Window.Owner <- argOwner

    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // OKボタンの押下
    member private this.OKButton_Click sender e =
        // 入力値を取得する
        rConfig.TCPReqResServer_DisableNagle <- m_DisableNagleCheck.IsChecked.GetValueOrDefault false

        rConfig.TCPReqResServer_ReceiveBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_ReceiveBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )
        rConfig.TCPReqResServer_SendBufferSize <-
            uint32(
                GlbFunc.NumRangeCheck
                    m_SendBufferSizeTextBox.Text
                    0L
                    2147483647L
                    8192L
                    ignore
            )

        m_Window.Close ()

    // Cancelボタンの押下
    member private this.CancelButton_Click sender e =
        // 保存せずに閉じる
        m_Window.Close ()
