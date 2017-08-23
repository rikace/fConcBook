module StockMarket

open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Reactive.Linq
open StockTicker.Core
open StockTicker.Server
open FSharp.Collections.ParallelSeq
open System.Reactive.Concurrency
open FSharp.Control.Reactive

//  Main "Controller" that run the Stock Market
type StockMarket (initStocks : Stock array) =   // #A
    
    // fake service-provider that tell "Stock-Ticker" that
    // is time to update the prices
    // I could use a real service... but probably at this time is closed
    // and it is good enough
    let ticker (stockAgent : Agent<StockTickerMessage>) =
            Observable.Interval(TimeSpan.FromMilliseconds 50.0)
            |> Observable.subscribe(fun _ -> stockAgent.Post UpdateStockPrices)

    let subject = new Subject<Trading>()   // #B
    static let instanceStockMarket = System.Lazy.Create(fun () -> StockMarket(Stock.InitialStocks()))

    // Agent resposible to upadte the stocks
    // open and close the market and dispatch the orders
    let stockMarketAgent =
        Agent<StockTickerMessage>.Start(fun inbox ->
            let rec marketIsOpen (stocks : Stock array)   // #F
                                 (stockTicker : IDisposable) = async { // #C
                    let! msg = inbox.Receive()
                    match msg with  // #D
                    | GetMarketState(c, reply) -> 
                        reply.Reply(MarketState.Open)
                        return! marketIsOpen stocks stockTicker   
                    | GetAllStocks(c, reply) -> 
                        reply.Reply(stocks |> Seq.toList)
                        return! marketIsOpen stocks stockTicker   
                    | UpdateStockPrices ->
                        stocks 
                        |> PSeq.iter(fun stock ->        // #H
                                    let isStockChanged = updateStocks stock stocks
                                    isStockChanged
                                    |> Option.iter(fun _ -> subject.OnNext(Trading.UpdateStock(stock))))
                        return! marketIsOpen stocks stockTicker   
                    | CloseMarket(c) ->
                        stockTicker.Dispose()
                        return! marketIsClosed stocks        
                    | _ -> return! marketIsOpen stocks stockTicker }
            and marketIsClosed (stocks : Stock array) = async {    // #F
                let! msg = inbox.Receive()
                match msg with  // #D
                | GetMarketState(c, reply) ->  reply.Reply(MarketState.Closed)  
                                               return! marketIsClosed stocks
                | GetAllStocks(c,reply) -> reply.Reply((stocks |> Seq.toList)) 
                                           return! marketIsClosed stocks
                | OpenMarket(c) -> return! marketIsOpen stocks (ticker inbox)
                | _ -> return! marketIsClosed stocks }
            marketIsClosed (initStocks))
            
    member x.GetAllStocks(connId) =
        stockMarketAgent.PostAndReply(fun ch -> GetAllStocks(connId, ch))

    member x.GetMarketState(connId) =
        stockMarketAgent.PostAndReply(fun ch -> GetMarketState(connId, ch))

    member x.OpenMarket(connId) =
        stockMarketAgent.Post(OpenMarket(connId))

    member x.CloseMarket(connId) =
        stockMarketAgent.Post(CloseMarket(connId))
    
    member x.AsObservable() = subject.AsObservable().SubscribeOn(TaskPoolScheduler.Default)  // #G
    
    static member Instance() = instanceStockMarket.Value