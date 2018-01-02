open System.Net
open System.Windows.Forms

[<EntryPoint>]
let main argv =
    ServicePointManager.DefaultConnectionLimit
        <- StockAnalyzer.Stocks.Length

    StockAnalyzer.showChart()
    |> Application.Run

    0