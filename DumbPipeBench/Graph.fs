///////////////////////////////////////////////////////////////////////////////
// Graph.fs : Graph表示画面
// 

namespace Graph

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Shapes
open System.Windows.Data
open System.Windows.Threading
open System.Collections
open System.Windows.Media
open Microsoft.Win32

type CGraph( argConfig : Config.CConfig, argData : CurrentPerfomanceData.CCCurrentPerfomanceData ) =

    //-------------------------------------------------------------------------
    // コンストラクタ

    // 引数に指定された定義を取得する
    let rConfig = argConfig
    let m_PerformData = argData
    
     // XAMLのコードをリソースからロードする
    let m_Window =
         Application.LoadComponent(
             new System.Uri( "/DumbPipeBench;component/Graph.xaml", UriKind.Relative )
         ) :?> Window
        
    // グラフの個数
    let m_GraphCount = 7

    // グラフに表示する時間枠（秒数）
    let m_GraphTimeSpan = 60

    // グラフの描画に使用する色
    let m_GraphColor_Dark = [|
        Brushes.Salmon;
        Brushes.SeaGreen;
        Brushes.SkyBlue;
        Brushes.HotPink;
        Brushes.Coral;
        Brushes.SteelBlue;
        Brushes.Salmon;
    |]
    let m_GraphColor_Light = [|
        Brushes.LightSalmon;
        Brushes.LightSeaGreen;
        Brushes.LightSkyBlue;
        Brushes.Pink;
        Brushes.LightCoral;
        Brushes.LightSteelBlue;
        Brushes.LightSalmon;
    |]

    // コントールのオブジェクトを取得する
    let m_GraphNameLabel = [|
        for i = 0 to m_GraphCount - 1 do
            yield m_Window.FindName( sprintf "GraphNameLabel_%02d" i ) :?> Label
    |]
    let m_GraphUnitLabel = [|
        for i = 0 to m_GraphCount - 1 do
            yield m_Window.FindName( sprintf "GraphUnitLabel_%02d" i ) :?> Label
    |]
    let m_GraphCanvas = [|
        for i = 0 to m_GraphCount - 1 do
            yield m_Window.FindName( sprintf "GraphCanvas_%02d" i ) :?> Canvas
    |]

    // グラフの枠
    let m_GraphLeftBorder = [|
        for i = 0 to m_GraphCount - 1 do
            let wL = new Line( Stroke = m_GraphColor_Dark.[i], StrokeThickness = 0.8 )
            yield wL
            m_GraphCanvas.[i].Children.Add wL |> ignore
    |]
    let m_GraphTopBorder = [|
        for i = 0 to m_GraphCount - 1 do
            let wL = new Line( Stroke = m_GraphColor_Dark.[i], StrokeThickness = 0.8 );
            yield wL
            m_GraphCanvas.[i].Children.Add wL |> ignore
    |]
    let m_GraphRightBorder = [|
        for i = 0 to m_GraphCount - 1 do
            let wL = new Line( Stroke = m_GraphColor_Dark.[i], StrokeThickness = 0.8 );
            yield wL
            m_GraphCanvas.[i].Children.Add wL |> ignore
    |]
    let m_GraphBottomBorder = [|
        for i = 0 to m_GraphCount - 1 do
            let wL = new Line( Stroke = m_GraphColor_Dark.[i], StrokeThickness = 0.8 );
            yield wL
            m_GraphCanvas.[i].Children.Add wL |> ignore
    |]

    // グラフ内の軸
    let m_HInsideLine = [|
        for i = 0 to m_GraphCount - 1 do
            yield [|
                for j = 0 to 8 do   // 10等分するため9本横線を引く
                    let wL = new Line( Stroke = m_GraphColor_Light.[i], StrokeThickness = 0.4 );
                    yield wL
                    m_GraphCanvas.[i].Children.Add wL |> ignore
            |]
    |]

    // グラフ内の多角形
    let m_GraphPolygon = [|
        for i = 0 to m_GraphCount - 1 do
            let wP =
                new Polygon(
                    Stroke = m_GraphColor_Dark.[i],
                    Fill = new SolidColorBrush( Color = m_GraphColor_Light.[i].Color, Opacity = 0.3 ),
                    StrokeThickness = 0.8
            )
            yield wP
            m_GraphCanvas.[i].Children.Add wP |> ignore
    |]

    // ポリゴン内の点の集合
    let m_GraphPolygon_PointCol = [|
        for i = 0 to m_GraphCount - 1 do
            let wC = new PointCollection( 63 );
            yield wC
            for j = 0 to m_GraphTimeSpan - 1 + 3 do
                wC.Add( Point( 0.0, 0.0 ) )
            m_GraphPolygon.[i].Points <- wC
    |]

    // タイマー
    let m_Timer = new DispatcherTimer()

    //-------------------------------------------------------------------------
    // 公開メソッド

    // ウインドウの初期化処理
    member this.Initialize argOwner =
        let wOwner = argOwner
        
        // イベントハンドラの登録
        for i = 0 to m_GraphCount - 1 do
            m_GraphCanvas.[i].SizeChanged.AddHandler ( fun sender e -> this.GraphCanvas_SizeChanged sender e i )

        // タイマーの初期化
        m_Timer.Interval <- new TimeSpan( 0, 0, 1 )    // 1秒ごと呼び出す
        m_Timer.Tick.AddHandler ( fun sender e -> this.OnTimer sender e )
        m_Timer.Start ()

        // 項目名および単位を初期化
        for i = 2 to 7 do
            m_GraphNameLabel.[ i - 2 ].Content <- m_PerformData.ColumnTitle( i )

        for i = 2 to 7 do
            m_GraphUnitLabel.[ i - 2 ].Content <- m_PerformData.ColumnUnitStr( i )

    // 表示
    member this.Show =
        m_Window.Show ()

    //-------------------------------------------------------------------------
    // イベントハンドラ

    // グラフのサイズが変更された
    member private this.GraphCanvas_SizeChanged sender e idx =
        let h = m_GraphCanvas.[idx].ActualHeight
        let w = m_GraphCanvas.[idx].ActualWidth

        // グラフの枠
        m_GraphLeftBorder.[idx].X1 <- 0.0
        m_GraphLeftBorder.[idx].X2 <- 0.0
        m_GraphLeftBorder.[idx].Y1 <- 0.0
        m_GraphLeftBorder.[idx].Y2 <- h

        m_GraphRightBorder.[idx].X1 <- w
        m_GraphRightBorder.[idx].X2 <- w
        m_GraphRightBorder.[idx].Y1 <- 0.0
        m_GraphRightBorder.[idx].Y2 <- h

        m_GraphTopBorder.[idx].X1 <- 0.0
        m_GraphTopBorder.[idx].X2 <- w
        m_GraphTopBorder.[idx].Y1 <- 0.0
        m_GraphTopBorder.[idx].Y2 <- 0.0

        m_GraphBottomBorder.[idx].X1 <- 0.0
        m_GraphBottomBorder.[idx].X2 <- w
        m_GraphBottomBorder.[idx].Y1 <- h
        m_GraphBottomBorder.[idx].Y2 <- h

        // グラフ内の横線
        for i = 1 to 9 do
            let r = m_HInsideLine.[idx].[i-1]
            r.X1 <- 0.0
            r.X2 <- w
            r.Y1 <- ( h / 10.0 ) * ( float i )
            r.Y2 <- ( h / 10.0 ) * ( float i )

        // グラフ本体の表示を更新する
        this.UpdateGraph idx

    // タイマイベント
    member this.OnTimer sender e =
        // グラフ全体を再描画する
        for i = 0 to m_GraphCount - 1 do
            this.UpdateGraph i

    //-------------------------------------------------------------------------
    // プライベートメソッド

    // グラフの表示を更新する
    member private this.UpdateGraph idx =
        let h = m_GraphCanvas.[idx].ActualHeight
        let w = m_GraphCanvas.[idx].ActualWidth

        // 値およびグラフのスケールを取得する
        let ( vVal : float[] ), ( Scale : float ) = this.GetValues idx

        for i = 0 to m_GraphTimeSpan - 1 do
            m_GraphPolygon_PointCol.[idx].Item(i) <-
                Point( w * float( i ) / float ( m_GraphTimeSpan - 1 ) , h * ( 1.0 - vVal.[i] / Scale ) )

        m_GraphPolygon_PointCol.[idx].Item( m_GraphTimeSpan - 1 + 1 ) <- Point( w, h )      // 右下
        m_GraphPolygon_PointCol.[idx].Item( m_GraphTimeSpan - 1 + 2 ) <- Point( 0.0, h )    // 左下
        m_GraphPolygon_PointCol.[idx].Item( m_GraphTimeSpan - 1 + 3 ) <- 
            m_GraphPolygon_PointCol.[idx].Item( 0 )                                         // 左上

        // 項目名
        m_GraphNameLabel.[ idx ].Content <-
            m_PerformData.ColumnTitle( idx + 2 )

        // 単位
        m_GraphUnitLabel.[ idx ].Content <-
            String.Format( "{0:#,0.######} ", Scale ) + m_PerformData.ColumnUnitStr( idx + 2 )

    // 表示する値を取得する
    member private this.GetValues idx : float[] * float =
        let v = Array.zeroCreate m_GraphTimeSpan
        let curItemCnt = m_PerformData.Items.Count
        for i = 0 to m_GraphTimeSpan - 1 do
            v.[ m_GraphTimeSpan - 1 - i ] <- 
                if curItemCnt > i then
                    let r = m_PerformData.Items.Item( curItemCnt - i - 1 )
                    match idx with
                    | 0 -> r.Connections_RV
                    | 1 -> r.TxBytes_RV * 8.0     // バイト単位の値をビット単位にする
                    | 2 -> r.RxBytes_RV * 8.0     // バイト単位の値をビット単位にする
                    | 3 -> r.Ext00_RV
                    | 4 -> r.Ext01_RV
                    | 5 -> r.Ext02_RV
                    | 6 -> r.Ext03_RV
                    | _ -> 0.0
                else
                    0.0
        v, 10.0 ** ceil( log10( Array.max v ) )
