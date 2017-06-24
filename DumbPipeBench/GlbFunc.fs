///////////////////////////////////////////////////////////////////////////////
// GlbFunc.fs : 共通関数の定義

module GlbFunc

/// 文字列長のチェック
let StrLengthCheck ( argStr : string ) ( argMaxLen : int ) errFunc : string =
    if argStr.Length > argMaxLen then
        let result = argStr.[ 0 .. argMaxLen - 1 ]
        errFunc result
        result
    else
        argStr

/// 数値のチェック
let NumRangeCheck ( argStr : string ) ( argMinVal : int64 ) ( argMaxVal : int64 ) ( defVal : int64 ) errFunc : int64 =
    let result, wVal = System.Int64.TryParse argStr
    if not result then
        // 数値に変換できなければ、デフォルト値を返す
        errFunc defVal
        defVal
    elif wVal < argMinVal then
        // 下限値を下回る場合は、下限値に切り上げる
        errFunc argMinVal
        argMinVal
    elif wVal > argMaxVal then
        // 上限値を上回る場合は、上限値まで切り捨てる
        errFunc argMaxVal
        argMaxVal
    else
        // 適切な範囲内に収まっているのであれば、入力値（数値）を返す
        wVal

