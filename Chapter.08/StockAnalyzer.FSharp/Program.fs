open System.Net
open StockAnalyzer.FSharp

[<EntryPoint>]
let main argv =
    ServicePointManager.DefaultConnectionLimit
        <- StockAnalyzerModule.Stocks.Length

    StockAnalyzerModule.showChart()
    
    0