module StockAnalyzer

open System
open System.IO
open System.Net
open System.Diagnostics
open FSharp.Charting
open FunctionalConcurrency

type StockData = {date:DateTime;open':float;high:float;low:float;close:float}

let Stocks = ["MSFT";"FB";"AAPL";"YHOO"; "EMC"; "AMZN"; "EBAY"; "INTC"; "GOOG"; "ORCL"; "SSY"]

let convertStockHistory (stockHistory:string) = async {
    let stockHistoryRows =
        stockHistory.Split(
            Environment.NewLine.ToCharArray(),
            StringSplitOptions.RemoveEmptyEntries)
    return
        stockHistoryRows
        |> Seq.skip 1
        |> Seq.map(fun row -> row.Split(','))
        // this is a guard against bad CSV row formatting when for example the stock index
        |> Seq.filter(fun cells -> cells |> Array.forall(fun c -> not <| (String.IsNullOrWhiteSpace(c) || (c.Length = 1 && c.[0] = '-'))))
        |> Seq.map(fun cells ->
            {
                date = DateTime.Parse(cells.[0]).Date
                open' = float(cells.[1])
                high = float(cells.[2])
                low = float(cells.[3])
                close = float(cells.[4])
            })
        |> Seq.toArray
}

let googleSourceUrl symbol =
    sprintf "https://finance.google.com/finance/historical?q=%s&output=csv" symbol

let yahooSourceUrl symbol =
    sprintf "http://ichart.finance.yahoo.com/table.csv?s=%s" symbol

let downloadStockHistory symbol = async {
    let url = googleSourceUrl symbol
    let req = WebRequest.Create(url)
    let! resp = req.AsyncGetResponse()
    use reader = new StreamReader(resp.GetResponseStream())
    return! reader.ReadToEndAsync()
}

let processStockHistory symbol = async {
    let! stockHistory = downloadStockHistory symbol
    let! stockData = convertStockHistory stockHistory
    return (symbol, stockData)
}

let chartSymbols (stockHistories:(string * StockData[])[])=
    let stockData = stockHistories |> Seq.map snd |> Seq.concat |> Seq.toArray
    let max = (stockData |> Seq.maxBy(fun x -> x.high)).high
    let min = (stockData |> Seq.minBy(fun x -> x.low)).low

    Chart.Combine
        [ for symbol, stockData in stockHistories ->
            let prices =
                stockData |> Seq.map (fun x->
                    x.date, x.open', x.high, x.low, x.close)
            Chart.Candlestick(prices, Name=symbol)
                 .WithYAxis(Max = float max, Min = float min)
        ]
        |> Chart.WithArea.AxisY
            ( Minimum = float min, Maximum = float max )
        |> Chart.WithLegend()


let analyzeStockHistory() =
    Stocks
    |> Seq.map (processStockHistory)
    |> Async.Parallel
    |> Async.RunSynchronously  // This is ok only for console applications, because block a better solution is Start with continuation
    |> chartSymbols


let showChart() =
    let chartP = analyzeStockHistory()
    chartP.ShowChart()
