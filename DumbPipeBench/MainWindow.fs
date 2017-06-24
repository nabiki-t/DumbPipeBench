///////////////////////////////////////////////////////////////////////////////
// MainWindow.fs : メインウインドウ全体を表すクラスの実装
// 


namespace MainWindow

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open Microsoft.Win32
open System.Text.RegularExpressions
open GlbFunc

type CMainWindow( argConfig : Config.CConfig ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/MainWindow.xaml", UriKind.Relative )
         ) :?> Window
    
    // コントールのオブジェクトを取得する
    let m_ResultList = m_Window.FindName( "ResultList" ) :?> ListView
    let m_TargetAddressLabel = m_Window.FindName( "TargetAddressLabel" ) :?> Label
    let m_TargetAddressTextBox = m_Window.FindName( "TargetAddressTextBox" ) :?> TextBox
    let m_PortNumberLabel = m_Window.FindName( "PortNumberLabel" ) :?> Label
    let m_PortNumberTextBox = m_Window.FindName( "PortNumberTextBox" ) :?> TextBox
    let m_ModeCombo = m_Window.FindName( "ModeCombo" ) :?> ComboBox
    let m_DetailButton = m_Window.FindName( "DetailButton" ) :?> Button
    let m_ResultLabel = m_Window.FindName( "ResultLabel" ) :?> Label
    let m_LogFileLabel = m_Window.FindName( "LogFileLabel" ) :?> Label
    let m_LogFileTextBox = m_Window.FindName( "LogFileTextBox" ) :?> TextBox
    let m_LogFileBrouseButton = m_Window.FindName( "LogFileBrouseButton" ) :?> Button
    let m_StartButton = m_Window.FindName( "StartButton" ) :?> Button
    let m_AutoScrollCheck = m_Window.FindName( "AutoScrollCheck" ) :?> CheckBox
    let m_GraphButton = m_Window.FindName( "GraphButton" ) :?> Button
    let m_FileExitMenu = m_Window.FindName( "Menu_File_Exit" ) :?> MenuItem
    let m_HelpAboutMenu = m_Window.FindName( "Menu_Help_About" ) :?> MenuItem

    // 測定データのカウンタ
    let m_Counter = new CurrentPerfomanceData.CCCurrentPerfomanceData()

    // コンボボックスへの値の設定
    let ModeComboIndex_TCPClient = m_ModeCombo.Items.Add "TCP Client"
    let ModeComboIndex_TCPServer = m_ModeCombo.Items.Add "TCP Server"
    let ModeComboIndex_TCPReqResClient = m_ModeCombo.Items.Add "TCP Request-Responce Client"
    let ModeComboIndex_TCPReqResServer = m_ModeCombo.Items.Add "TCP Request-Responce Server"
    let ModeComboIndex_UDPClient = m_ModeCombo.Items.Add "UDP"

    // 通信用のオブジェクト
    let mutable m_Stresser : option<Stresser.CStresser> = None

    // タイマー
    let m_Timer = new DispatcherTimer()

    //-------------------------------------------------------------------------
    // メソッド

    // ウインドウの初期化処理
    member this.Initialize () =
        // ボタンのイベントハンドラを追加している
        // 単にCMainWindowのメンバを呼び出すようにしているだけ
        m_ModeCombo.SelectionChanged.AddHandler ( fun sender e -> this.ModeCombo_SelChanged sender e )
        m_DetailButton.Click.AddHandler ( fun sender e -> this.DetailButton_Click sender e )
        m_StartButton.Click.AddHandler ( fun sender e -> this.StartButton_Click sender e )
        m_LogFileBrouseButton.Click.AddHandler ( fun sender e -> this.LogFileBrouseBtn_Click sender e )
        m_Window.Closed.AddHandler ( fun sender e -> this.OnClosed sender e )
        m_GraphButton.Click.AddHandler ( fun sender e -> this.GraphButton_Click sender e )
        m_FileExitMenu.Click.AddHandler ( fun sender e -> this.FileExitMenu_Click sender e )
        m_HelpAboutMenu.Click.AddHandler ( fun sender e -> this.HelpAboutMenu_Click sender e )

        // 初期値を設定する（モード）
        match rConfig.RunMode with
        | Constant.TCPClient ->
            m_ModeCombo.SelectedIndex <-  ModeComboIndex_TCPClient
            m_TargetAddressTextBox.IsEnabled <- true
        | Constant.TCPServer ->
            m_ModeCombo.SelectedIndex <-  ModeComboIndex_TCPServer
            m_TargetAddressTextBox.IsEnabled <- false
        | Constant.TCPReqResClient ->
            m_ModeCombo.SelectedIndex <-  ModeComboIndex_TCPReqResClient
            m_TargetAddressTextBox.IsEnabled <- true
        | Constant.TCPReqResServer ->
            m_ModeCombo.SelectedIndex <-  ModeComboIndex_TCPReqResServer
            m_TargetAddressTextBox.IsEnabled <- false
        | Constant.UDPClient ->
            m_ModeCombo.SelectedIndex <-  ModeComboIndex_UDPClient
            m_TargetAddressTextBox.IsEnabled <- true

        // 初期値を設定する（接続先アドレス）
        m_TargetAddressTextBox.Text <- rConfig.TargetAddress

        // 初期値を設定する（ポート番号）
        m_PortNumberTextBox.Text <- sprintf "%d" rConfig.PortNumber

        // 初期値を設定する（ログファイル名）
        m_LogFileTextBox.Text <- rConfig.LogFileName

        // 初期値を設定する（自動スクロール名）
        m_AutoScrollCheck.IsChecked <- Nullable rConfig.AutoScroll

        // 結果リストの初期化
        m_Counter.Clear ()
        m_ResultList.DataContext <- new CollectionViewSource( Source = m_Counter.Items )

        // 結果リストのカラムの幅を設定する
        let rCols = ( m_ResultList.View :?> GridView ).Columns
        for i = 0 to rCols.Count - 1 do
            rCols.Item( i ).Width <- rConfig.ListColumnWidth i
        
        // タイマーの初期化
        m_Timer.Interval <- new TimeSpan( 0, 0, 1 )    // 1秒ごと呼び出す
        m_Timer.Tick.AddHandler ( fun sender e -> this.OnTimer sender e )
        m_Timer.Start ()

    // 表示
    member this.Show ( apl : Application ) =
        apl.ShutdownMode <- ShutdownMode.OnMainWindowClose
        apl.Run m_Window

    //-------------------------------------------------------------------------
    // プライベートメソッド

    // 入力された設定値を取得する
    member private this.GetInputValueToConf () =

        // モード
        let CurIdx = m_ModeCombo.SelectedIndex
        rConfig.RunMode <- 
            snd ( [|
                    ( ModeComboIndex_TCPClient, Constant.TCPClient );
                    ( ModeComboIndex_TCPServer, Constant.TCPServer );
                    ( ModeComboIndex_TCPReqResClient, Constant.TCPReqResClient );
                    ( ModeComboIndex_TCPReqResServer, Constant.TCPReqResServer );
                    ( ModeComboIndex_UDPClient, Constant.UDPClient );
            |] |> Array.find ( fun v -> fst v = CurIdx ) )

        // 接続先アドレス
        rConfig.TargetAddress <- GlbFunc.StrLengthCheck m_TargetAddressTextBox.Text 32768 ( fun v -> m_TargetAddressTextBox.Text <- v )

        // ポート番号
        rConfig.PortNumber <- uint16( GlbFunc.NumRangeCheck m_PortNumberTextBox.Text 1L 65535L 60000L ( fun v -> m_PortNumberTextBox.Text <- string v ) )

        // ログファイル名
        rConfig.LogFileName <- GlbFunc.StrLengthCheck m_LogFileTextBox.Text 32768 ( fun v -> m_LogFileTextBox.Text <- v )

        // 自動スクロール
        rConfig.AutoScroll <- ( m_AutoScrollCheck.IsChecked.HasValue && m_AutoScrollCheck.IsChecked.Value )

    //-------------------------------------------------------------------------
    // イベントハンドラ
 
    // ウインドウが閉じられる
    member private this.OnClosed sender e =
        // 入力された値を取得する
        this.GetInputValueToConf ()

        // リストのカラム幅を保存する
        let rCols = ( m_ResultList.View :?> GridView ).Columns
        for i = 0 to rCols.Count - 1 do
            rConfig.ListColumnWidth i <- rCols.Item( i ).Width

        // レジストリに保存する
        rConfig.SaveRegistory ()

    // 詳細ボタンの押下
    member private this.DetailButton_Click sender e =
        // 現在選択されているモードの指定に応じて、詳細設定画面を表示する
        if m_ModeCombo.SelectedIndex = ModeComboIndex_TCPClient then
            let w = new ConfTcpClient.CConfTcpClient( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore
        elif m_ModeCombo.SelectedIndex = ModeComboIndex_TCPServer then
            let w = new ConfTcpServer.CConfTcpServer( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore
        elif m_ModeCombo.SelectedIndex = ModeComboIndex_TCPReqResClient then
            let w = new ConfTcpReqResClient.CConfTcpReqResClient( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore
        elif m_ModeCombo.SelectedIndex = ModeComboIndex_TCPReqResServer then
            let w = new ConfTcpReqResServer.CConfTcpReqResServer( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore
        elif m_ModeCombo.SelectedIndex = ModeComboIndex_UDPClient then
            let w = new ConfUdpClient.CConfUdpClient( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore
        else
            let w = new ConfTcpClient.CConfTcpClient( rConfig )
            w.Initialize m_Window
            w.Show () |> ignore

    // Modeコンボボックスの選択が変更された時に呼ばれる
    member private this.ModeCombo_SelChanged sender e =
        let CurIdx = m_ModeCombo.SelectedIndex
        let idx, en =
            [|  ( ModeComboIndex_TCPClient, true );
                ( ModeComboIndex_TCPServer, false );
                ( ModeComboIndex_TCPReqResClient, true );
                ( ModeComboIndex_TCPReqResServer, false );
                ( ModeComboIndex_UDPClient, true );
            |]
            |> Array.find ( fun v -> fst v = CurIdx )
        m_TargetAddressTextBox.IsEnabled <- en

    // Startボタン押下時に呼ばれる
    member private this.StartButton_Click sender e =
        
        // 実行中か否か
        match m_Stresser with
        | None ->
            // 実行中ではないので、これから開始する

            // 入力された値を取得する
            this.GetInputValueToConf ()

            // 結果リストを初期化する
            m_Counter.Clear ()

            let rCols = ( m_ResultList.View :?> GridView ).Columns

            // 通信用のオブジェクトを構築する
            match rConfig.RunMode with
            | Constant.TCPClient ->
                m_Stresser <- Some( new Stresser.CTCPClient.CTCPClient( rConfig ) :> Stresser.CStresser )
            | Constant.TCPServer ->
                m_Stresser <- Some( new Stresser.CTCPServer.CTCPServer( rConfig ) :> Stresser.CStresser )
            | Constant.TCPReqResClient ->
                m_Stresser <- Some( new Stresser.CTCPReqResClient.CTCPReqResClient( rConfig ) :> Stresser.CStresser )
            | Constant.TCPReqResServer ->
                m_Stresser <- Some( new Stresser.CTCPReqResServer.CTCPReqResServer( rConfig ) :> Stresser.CStresser )
            | Constant.UDPClient ->
                m_Stresser <- Some( new Stresser.CUDPClient.CUDPClient( rConfig ) :> Stresser.CStresser )
            
            // 項目名の文字列を設定する
            let vTitle =
                match rConfig.RunMode with
                | Constant.TCPClient ->
                    [| "#"; "Time"; "Connections"; "TxBytes/s"; "RxBytes/s"; "N/A"; "N/A"; "N/A"; "N/A"; |]
                | Constant.TCPServer ->
                    [| "#"; "Time"; "Connections"; "TxBytes/s"; "RxBytes/s"; "N/A"; "N/A"; "N/A"; "N/A"; |]
                | Constant.TCPReqResClient ->
                    [| "#"; "Time"; "Connections"; "TxBytes/s"; "RxBytes/s"; "RTT(avg.)"; "RTT Jitter"; "Req-Res Count"; "N/A"; |]
                | Constant.TCPReqResServer ->
                    [| "#"; "Time"; "Connections"; "TxBytes/s"; "RxBytes/s"; "Req-Res Count"; "N/A"; "N/A"; "N/A"; |]
                | Constant.UDPClient ->
                    [| "#"; "Time"; "Connections"; "TxBytes/s"; "RxBytes/s"; "TxPackets/s"; "RxPackets/s"; "Jitter"; "DropPackets/s"; |]
            for i = 0 to 8 do
                m_Counter.ColumnTitle( i ) <- vTitle.[i]
                rCols.Item( i ).Header <- m_Counter.ColumnTitle( i )

            // 項目ごとの単位の文字列を設定する
            let vUnit =
                match rConfig.RunMode with
                | Constant.TCPClient ->
                    [| ""; ""; "Connections"; "bit/sec"; "bit/sec"; "N/A"; "N/A"; "N/A"; "N/A"; |]
                | Constant.TCPServer ->
                    [| ""; ""; "Connections"; "bit/sec"; "bit/sec"; "N/A"; "N/A"; "N/A"; "N/A"; |]
                | Constant.TCPReqResClient ->
                    [| ""; ""; "Connections"; "bit/sec"; "bit/sec"; "ms"; "ms"; "Counts"; "N/A"; |]
                | Constant.TCPReqResServer ->
                    [| ""; ""; "Connections"; "bit/sec"; "bit/sec"; "Counts"; "N/A"; "N/A"; "N/A"; |]
                | Constant.UDPClient ->
                    [| ""; ""; "Connections"; "bit/sec"; "bit/sec"; "Packets"; "Packets"; "ms"; "Packets"; |]
            for i = 0 to 8 do
                m_Counter.ColumnUnitStr( i ) <- vUnit.[i]

            // 開始する
            ( Option.get m_Stresser ).Start ()

            // ボタンの名称を変更する
            m_StartButton.Content <- "_Stop"

            // コントロールを無効化する
            m_TargetAddressTextBox.IsEnabled <- false
            m_PortNumberTextBox.IsEnabled <- false
            m_ModeCombo.IsEnabled <- false
            m_DetailButton.IsEnabled <- false
            m_LogFileTextBox.IsEnabled <- false
            m_LogFileBrouseButton.IsEnabled <- false

        | Some( p ) ->
            // 実行中であるため停止する
            p.Stop ()

            m_Stresser <- None

            // ボタンの名称を変更する
            m_StartButton.Content <- "_Start"

            // コントロールを有効化する
            match rConfig.RunMode with
            | Constant.TCPClient ->
                m_TargetAddressTextBox.IsEnabled <- true
            | Constant.TCPServer ->
                m_TargetAddressTextBox.IsEnabled <- false
            | Constant.TCPReqResClient ->
                m_TargetAddressTextBox.IsEnabled <- true
            | Constant.TCPReqResServer ->
                m_TargetAddressTextBox.IsEnabled <- false
            | Constant.UDPClient ->
                m_TargetAddressTextBox.IsEnabled <- true
            m_PortNumberTextBox.IsEnabled <- true
            m_ModeCombo.IsEnabled <- true
            m_DetailButton.IsEnabled <- true
            m_LogFileTextBox.IsEnabled <- true
            m_LogFileBrouseButton.IsEnabled <- true

    // グラフ表示ボタン押下時に呼ばれる
    member private this.GraphButton_Click sender e =
        // グラフ表示用のウインドウ
        let m_GraphWindow = new Graph.CGraph( rConfig, m_Counter )
        m_GraphWindow.Initialize this
        m_GraphWindow.Show

    // フォルダ参照ボタン押下時に呼ばれる
    member private this.LogFileBrouseBtn_Click sender e =
        let dlg =
            new SaveFileDialog(
                FileName = "DumbPipeBench_" + DateTime.Now.ToLocalTime().ToString( "yyyy-MM-dd HH-mm-ss" ) + ".txt",
                Filter = "Text Files|*.txt|All Files|*.*"
            )
        if dlg.ShowDialog().GetValueOrDefault( false ) then
            m_LogFileTextBox.Text <- dlg.FileName 

    // Exitメニューの押下
    member private this.FileExitMenu_Click sender e =
        // 終了する
        Application.Current.Shutdown 0

    // Aboutメニューの押下
    member private this.HelpAboutMenu_Click sender e =
        let w = new About.CAbout()
        w.Initialize m_Window
        w.Show () |> ignore

    // タイマイベント
    // （実行中か否かにかかわらず、常時呼び出される）
    member private this.OnTimer sender e =
        // 実行中か否か
        match m_Stresser with
        | None ->
            // 実行中ではないため無視する
            ()
        | Some( p ) ->
            // 実行中のため、測定値の取得を指示する

            // 測定を取得する
            let perfData = p.Pop ()

            if perfData.Count = 0 then
                // 0件の場合は、現在時刻のレコードを作り出すしかない
                m_Counter.Add 
                    m_LogFileTextBox.Text
                    {
                        PIV_ScanCount = 1u
                        PIV_Time = DateTime.Now
                        PIV_Connections = p.ConnectionCount
                        PIV_TxBytes = 0UL
                        PIV_RxBytes = 0UL
                        PIV_LatencySum = 0.0
                        PIV_LatencyCount = 0u
                        PIV_Jitter = 0.0
                        PIV_ReqCount = 0u
                        PIV_ResCount = 0u
                        PIV_DropCount = 0u
                    }
                    p.RunMode
            else
                for itr in perfData do
                    let wVal = ( itr :?> DictionaryEntry ).Value :?> CurrentPerfomanceData.PerfomItemVal
                    m_Counter.Add m_LogFileTextBox.Text wVal p.RunMode
            
            // 自動スクロールが有効な場合は、リストを一番下にスクロールする
            if m_AutoScrollCheck.IsChecked.HasValue && m_AutoScrollCheck.IsChecked.Value then
                m_ResultList.ScrollIntoView( m_ResultList.Items.Item( m_ResultList.Items.Count - 1 ) )

