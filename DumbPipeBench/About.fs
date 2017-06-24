///////////////////////////////////////////////////////////////////////////////
// About.fs : UDPClient用詳細設定画面
// 

namespace About

open System
open System.Windows
open System.Windows.Controls

type CAbout() =

    //-------------------------------------------------------------------------
    // コンストラクタ
   
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/About.xaml", UriKind.Relative )
         ) :?> Window
        
    // コントールのオブジェクトを取得する
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button

    //-------------------------------------------------------------------------
    // メソッド

    // ウインドウの初期化処理
    member this.Initialize argOwner =
        // イベントハンドラの追加
        m_OKButton.Click.AddHandler ( fun sender e -> this.OKButton_Click sender e )

        m_Window.Owner <- argOwner

    // 表示
    member this.Show () =
        m_Window.ShowDialog().GetValueOrDefault( false )

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // OKボタンの押下
    member private this.OKButton_Click sender e =
        m_Window.Close ()
