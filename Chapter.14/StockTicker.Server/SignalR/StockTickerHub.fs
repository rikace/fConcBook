module StockTickerHub

open Owin
open Microsoft.Owin
open System
open System.Collections.Generic
open System.Web
open System.Threading
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open System.Reactive.Subjects
open System.Reactive.Linq
open FSharp.Collections.ParallelSeq
open TradingAgent
open TradingSupervisorAgent
open StockMarket
open StockTicker.Core
open StockTicker.Logging
open StockTicker.Logging.Message

let logger = Log.create "StockTicker.Hub"

// Listing 14.10 Stock Ticker SignalR hub
// Hub with "Stock Market" operations
// It is responsible to update the stock-ticker
// to each client registered
[<HubName("stockTicker")>] // #A
type StockTickerHub() as this =
    inherit Hub<IStockTickerHubClient>() // #B

    static let userCount = ref 0

    let stockMarket : StockMarket = StockMarket.Instance() // #C
    let tradingCoordinator : TradingCoordinator = TradingCoordinator.Instance() // #C

    let logUserEvent eventName connId =
        logger.logWithAck Info (
           eventX "{logger} event {event}!\t User {connId}. There are {total} connected users."
           >> setField "logger" (sprintf "%A" logger.name)
           >> setField "event" eventName
           >> setField "connId" connId
           >> setField "total" !userCount )
        |> Async.StartImmediate
    let logUserCall connId methodName =
        logger.logWithAck Info (
           eventX "{logger}: User {connId} called {methodName}"
           >> setField "logger" (sprintf "%A" logger.name)
           >> setField "connId" connId
           >> setField "methodName" methodName )
        |> Async.StartImmediate

    override x.OnConnected() = // #D
        ignore <| System.Threading.Interlocked.Increment(userCount)
        let connId = x.Context.ConnectionId
        logUserEvent "OnConnected" connId

        // Subscribe a new client
        // I can use this.Clients.Caller but this is demo purpose
        tradingCoordinator.Subscribe(connId, 1000., this.Clients) // #E
        base.OnConnected()

    override x.OnDisconnected(stopCalled) =  // #D
        ignore <| System.Threading.Interlocked.Decrement(userCount)
        let connId = x.Context.ConnectionId
        logUserEvent "OnDisconnected" connId

        // un-subscribe client
        tradingCoordinator.Unsubscribe(connId) // #E
        base.OnDisconnected(stopCalled)

    member x.GetAllStocks() = // #F
        let connId = x.Context.ConnectionId
        logUserCall connId "GetAllStocks"

        let stocks = stockMarket.GetAllStocks(connId)
        for stock in stocks do
            this.Clients.Caller.SetStock stock

    member x.OpenMarket() = // #F
        let connId = x.Context.ConnectionId
        logUserCall connId "OpenMarket"

        stockMarket.OpenMarket(connId)
        this.Clients.All.SetMarketState(MarketState.Open.ToString())

    member x.CloseMarket() = // #F
        let connId = x.Context.ConnectionId
        logUserCall connId "CloseMarket"

        stockMarket.CloseMarket(connId)
        this.Clients.All.SetMarketState(MarketState.Closed.ToString())

    member x.GetMarketState() = // #F
        let connId = x.Context.ConnectionId
        logUserCall connId "GetMarketState"

        stockMarket.GetMarketState(connId).ToString()
