module StockMarket

open System
open System.Reactive.Subjects
open System.Reactive.Linq
open StockTicker.Core
open StockTicker.Server
open FSharp.Collections.ParallelSeq
open System.Reactive.Concurrency
open FSharp.Control.Reactive

// Listing 14.6 StockMarket type based on agent to coordinate the TradingSupervisor
// Main "Controller" that run the Stock Market
type StockMarket (initStocks : Stock array) =   // #A

    // fake service-provider that tell "Stock-Ticker" that
    // is time to update the prices
    // I could use a real service... but probably at this time is closed
    // and it is good enough
    // Listing 14.7 Function to randomly update the stock ticker prices
    let startStockTicker (stockAgent : Agent<StockTickerMessage>) =
        Observable.Interval(TimeSpan.FromMilliseconds 50.0)
        |> Observable.subscribe(fun _ -> stockAgent.Post UpdateStockPrices)

    let subject = new Subject<Trading>()   // #B
    static let instanceStockMarket =
        System.Lazy.Create(fun () -> StockMarket(Stock.InitialStocks()))

    // Agent responsible to update the stocks
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
                | UpdateStockPrices ->  // #E
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
                | GetMarketState(c, reply) ->
                    reply.Reply(MarketState.Closed)
                    return! marketIsClosed stocks
                | GetAllStocks(c,reply) ->
                    reply.Reply((stocks |> Seq.toList))
                    return! marketIsClosed stocks
                | OpenMarket(c) ->
                    return! marketIsOpen stocks (startStockTicker inbox)
                | _ -> return! marketIsClosed stocks }
            marketIsClosed (initStocks))

    member this.GetAllStocks(connId) =
        stockMarketAgent.PostAndReply(fun ch -> GetAllStocks(connId, ch))

    member this.GetMarketState(connId) =
        stockMarketAgent.PostAndReply(fun ch -> GetMarketState(connId, ch))

    member this.OpenMarket(connId) =
        stockMarketAgent.Post(OpenMarket(connId))

    member this.CloseMarket(connId) =
        stockMarketAgent.Post(CloseMarket(connId))

    member this.AsObservable() =
        subject.AsObservable().SubscribeOn(TaskPoolScheduler.Default)  // #G

    static member Instance() = instanceStockMarket.Value