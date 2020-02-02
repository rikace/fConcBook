namespace StockTicker.Core

open System
open System.Collections.Generic
open System.IO
open System.Net

[<AutoOpenAttribute>]
module Models =
    // using the CLIMutable attribute the Record type
    // is serializable by other .NET languages/frameworks
    // while the type is still immutable by F#
    
    [<CLIMutable>]
    type User =
        { UserId: string
          Username: string
          Initialcash: decimal }

    [<CLIMutable>]
    type Order =
        { UserId: string
          ConnId: string
          Symbol: string
          Quantity: int
          TradingType: string
          Price: string }

    type TradingType =
        | Buy
        | Sell
           
    [<CLIMutable>]
    type ClientOrder =
        { UserId:      string
          Symbol:      string
          Quantity:    int
          Price:       decimal
          TradingType: TradingType }
        
    type TradingCommand =
        | BuyStockCommand of connectionId: string * tradingRecord: ClientOrder
        | SellStockCommand of connectionId: string * tradingRecord: ClientOrder

    and [<CLIMutableAttribute>] TickerRecord =
        { Symbol: string
          Price: decimal }

    [<CLIMutable>]
    type CommandWrapper =
        { ConnectionId: string
          Id: Guid
          Created: DateTimeOffset
          Command: TradingCommand }

        static member CreateTrading connectionId (item: ClientOrder) =
            let command =
                match item.TradingType with
                | Buy -> BuyStockCommand(connectionId, item)
                | Sell -> SellStockCommand(connectionId, item)
            { Id = (Guid.NewGuid())
              Created = (DateTimeOffset.Now)
              ConnectionId = connectionId
              Command = command }

    type StockInitInfo = {
        Symbol : string
        InitialValue : decimal
        Volatility : float
    }
    
    [<RequireQualifiedAccess>]
    module Stocks =
        let stocks =
            [| { StockInitInfo.Symbol = "MSFT"; InitialValue = 58.68M;  Volatility = 0.19 }
               { StockInitInfo.Symbol = "APPL"; InitialValue = 92.08M;  Volatility = 0.20 }
               { StockInitInfo.Symbol = "AMZN"; InitialValue = 380.15M; Volatility = 0.21 }
               { StockInitInfo.Symbol = "GOOG"; InitialValue = 543.01M; Volatility = 0.18 }
               { StockInitInfo.Symbol = "ORCL"; InitialValue = 48.62M;  Volatility = 0.19 }
               { StockInitInfo.Symbol = "INTC"; InitialValue = 35.02M;  Volatility = 0.20 }
               { StockInitInfo.Symbol = "CSCO"; InitialValue = 30.07M;  Volatility = 0.18 }
               { StockInitInfo.Symbol = "FB";   InitialValue = 78.97M;  Volatility = 0.20 } |]
    
    [<CLIMutableAttribute>]
    type Stock =
        { Symbol: string
          DayOpen: decimal
          DayLow: decimal
          DayHigh: decimal
          LastChange: decimal
          Price: decimal
          Date: DateTime }
        member x.Change = x.Price - x.DayOpen
        member x.PercentChange = double (Math.Round(x.Change / x.Price, 4))
        static member Create (symbol: string) price =
            { Symbol = symbol
              LastChange = 0M
              Price = price
              DayOpen = 0M
              DayLow = 0M
              DayHigh = 0M
              Date = DateTime.Today }

        // List of arbitrary stock tickers
        // For Demo purpose
        static member InitialStocks() =
            Stocks.stocks
            |> Array.map (fun ({ StockInitInfo.Symbol = ticker; InitialValue = price; }) -> Stock.Create ticker price)

        static member InitialStocks(stockTicker: string list) =
            let getStockIndex ticker =
                async {
                    let url = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=snl1" ticker
                    let req = WebRequest.Create(url)
                    let! resp = req.AsyncGetResponse()
                    use reader = new StreamReader(resp.GetResponseStream())
                    let! row = reader.ReadToEndAsync() |> Async.AwaitTask
                    let items = row.Split(',')
                    let price = Double.Parse(items.[items.Length - 1])
                    return (ticker, price)
                }
            stockTicker
            |> Seq.map getStockIndex
            |> Async.Parallel

    type MarketState =
        | Closed
        | Open
        override this.ToString() =
            match this with
            | Open -> "Open"
            | Closed -> "Closed"

    [<CLIMutable>]
    type OrderRecord =
        { OrderId: Guid
          Symbol: string
          Quantity: int
          Price: decimal
          TradingType: string  }
        
    type StockTickerMessage =
        | UpdateStockPrices
        | OpenMarket of string
        | CloseMarket of string
        | GetMarketState of string * AsyncReplyChannel<MarketState>
        | GetAllStocks of string * AsyncReplyChannel<Stock list>

    type Trading =
        | Kill of AsyncReplyChannel<unit>
        | Error of exn
        | Buy of symbol: string * ClientOrder
        | Sell of symbol: string * ClientOrder
        | UpdateStock of Stock

    and Treads = Dictionary<string, ResizeArray<OrderRecord>>
    and Portfolio = IDictionary<string, OrderRecord>

    [<CLIMutable>]
    type Asset =
        { Cash: decimal
          Portfolio: Portfolio
          BuyOrders: Treads
          SellOrders: Treads }
            static member Default =
                { Cash = 0M
                  Portfolio = Dictionary<_,_>(HashIdentity.Structural) :> IDictionary<_,_>
                  SellOrders = Treads(HashIdentity.Structural)
                  BuyOrders = Treads(HashIdentity.Structural) }


    [<CLIMutable>]
    type PredictionRequest =
        { Symbol: string
          Price: decimal
          NumTimesteps: int }

    [<CLIMutable>]
    type PredictionResponse =
        { MeanPrice: decimal
          Quartiles: float [] }
