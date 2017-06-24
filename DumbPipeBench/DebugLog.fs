///////////////////////////////////////////////////////////////////////////////
// DebugLog.fs : デバッグ用のログ出力処理

module DebugLog

open System
open System.IO

#if DEBUG

// 出力先のファイル
//let rLogFile = ref ( File.CreateText @"D:\dbglog.txt"  )

#endif

/// ログにメッセージを出力する
let OutputMsg ( msg : string ) =

#if DEBUG
(*    // デバッグ用にファイルにメッセージを出力する
    lock ( rLogFile ) (
        fun () ->
            let dt = System.DateTime.Now
            ( !rLogFile ).WriteLine ( sprintf "%04d/%02d/%02d %02d:%02d:%02d.%03d : %s\n" dt.Year dt.Month dt.Day dt.Hour dt.Minute dt.Second dt.Millisecond msg )
            ( !rLogFile ).Flush ()
            ()
    )
*)
    ()
#else
    // 何もしない
    ()
#endif


/// ログファイルをクローズする
let Close () =
#if DEBUG
//    (!rLogFile).Close()
    ()
#else
    // 何もしない
    ()
#endif
