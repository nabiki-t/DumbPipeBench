///////////////////////////////////////////////////////////////////////////////
// DumbPipeBench.fs DumbPipeBenchのエントリポイント
// 

module DumbPipeBench

open System
open System.Windows
open System.Threading

/// <summary>エントリポイント。ウインドウを表示するのみ。</summary>
/// <param name="argv">起動時の引数。使用しない。</param>
[<STAThread>]
[<EntryPoint>]
let main argv = 
    // ウインドウのクラスを構築
    let WinObj = new MainWindow.CMainWindow( new Config.CConfig() )

    // 初期化する
    WinObj.Initialize ()

    // 実行してやる
    new Application() |> WinObj.Show

