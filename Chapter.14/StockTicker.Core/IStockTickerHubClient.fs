namespace StockTicker.Core

open System.Threading.Tasks

// interface StockTickerHub SignalR
type IStockTickerHubClient =
    abstract GetMarketState : unit -> Task<string>
    abstract OpenMarket : unit -> unit
    abstract CloseMarket : unit -> unit    
    abstract GetAllStocks : unit -> Task
    abstract Subscribe : string * decimal -> Task