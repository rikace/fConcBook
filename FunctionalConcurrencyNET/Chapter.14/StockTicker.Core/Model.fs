namespace StockTicker.Core

open System
open System.Collections.Generic
open System.IO
open System.Net

[<AutoOpenAttribute>]
module Models =
    type TradingType =
    | Buy
    | Sell

    [<CLIMutable>]
    [<Struct>]
    // using the CLIMutable attribute the Record type
    // is serializable by other .NET languages/frameworks
    // while the type is still immutable by F#
    type OrderRecord =
        { Symbol : string
          Quantity : int
          Price : float
          OrderType : TradingType }

    [<CLIMutable>]
    type Asset = { Cash:float; Portfolio:List<OrderRecord>; BuyOrders:List<OrderRecord>; SellOrders:List<OrderRecord> }

    [<CLIMutable>]
    type TradingRequest =
        { ConnectionID : string
          Symbol : string
          Price : float
          Quantity : int }

    [<CLIMutable>]
    type TradingRecord =
        { Symbol : string
          Quantity : int
          Price : float
          Trading : TradingType }

    type TradingCommand =
        | BuyStockCommand  of connectionId : string * tradingRecord : TradingRecord
        | SellStockCommand of connectionId : string * tradingRecord : TradingRecord

    and [<CLIMutableAttribute>]
        TickerRecord =
        { Symbol : string
          Price : float }

    [<CLIMutable>]
    type CommandWrapper =
        { ConnectionId : string
          Id : Guid
          Created : DateTimeOffset
          Command : TradingCommand }

        static member CreateTrading connectionId (item : TradingRecord) =
            let command =
                match item.Trading with
                | Buy -> BuyStockCommand(connectionId, item)
                | Sell -> SellStockCommand(connectionId, item)
            { Id = (Guid.NewGuid())
              Created = (DateTimeOffset.Now)
              ConnectionId = connectionId
              Command = command }

    [<CLIMutableAttribute>]
    type Stock =
        { Symbol : string
          DayOpen : float
          DayLow : float
          DayHigh : float
          LastChange : float
          Price : float
          Index : int }
        member x.Change = x.Price - x.DayOpen

        member x.PercentChange = double (Math.Round(x.Change / x.Price, 4))

        static member Create (symbol : string) price index =
            { Symbol = symbol
              LastChange = 0.
              Price = price
              DayOpen = 0.
              DayLow = 0.
              DayHigh = 0.
              Index = index }

        // For Demo purpose
        static member InitialStocks() =
            [| ("MSFT", 58.68)
               ("APPL", 92.08)
               ("AMZN", 380.15)
               ("GOOG", 543.01)
               ("ORCL", 48.62)
               ("INTC", 35.02)
               ("CSCO", 30.07)
               ("FB", 78.97) |]
            |> Array.mapi(fun index (ticker, price) -> Stock.Create ticker price index)

        static member InitialStocks(stockTicker:string list) =
            let getStockIndex index ticker = async {
                    let url = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=snl1" ticker
                    let req = WebRequest.Create(url)
                    let! resp = req.AsyncGetResponse()
                    use reader = new StreamReader(resp.GetResponseStream())
                    let! row = reader.ReadToEndAsync() |> Async.AwaitTask
                    let items = row.Split(',')
                    let price = Double.Parse(items.[items.Length-1])
                    return (ticker, price, index)
            }
            stockTicker
            |> Seq.mapi(getStockIndex)
            |> Async.Parallel

    type MarketState =
        | Closed
        | Open
        override this.ToString() =
            match this with
            | Open -> "Open"
            | Closed -> "Closed"

    type StockTickerMessage =
        | UpdateStockPrices
        | OpenMarket of string
        | CloseMarket of string
        | GetMarketState of string * AsyncReplyChannel<MarketState>
        | GetAllStocks of string * AsyncReplyChannel<Stock list>


    type Trading =
        | Kill of AsyncReplyChannel<unit>
        | Error of exn
        | Buy of symbol : string * TradingDetails
        | Sell of symbol : string * TradingDetails
        | UpdateStock of Stock

    and TradingDetails =
        { Quantity : int
          Price : float
          TradingType:TradingType }

    and Treads = Dictionary<string, ResizeArray<TradingDetails>>

    and Portfolio = Dictionary<string, TradingDetails>

    [<CLIMutable>]
    type PredictionRequest =
        { Symbol : string
          Price : float
          NumTimesteps : int }
    [<CLIMutable>]
    type PredictionResponse =
        { MeanPrice: float
          Quartiles: float[] }