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

 // Hub with "Stock Market" operations
 // It is responsible to update the stock-ticker
 // to each client registered
[<HubName("stockTicker")>]
type StockTickerHub() as this =
    inherit Hub<IStockTickerHubClient>()

    static let userCount = ref 0

    let stockMarket : StockMarket = StockMarket.Instance()
    let tradingCoordinator : TradingCoordinator = TradingCoordinator.Instance()

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

    override x.OnConnected() =
        ignore <| System.Threading.Interlocked.Increment(userCount)
        let connId = x.Context.ConnectionId
        logUserEvent "OnConnected" connId

        // Subscribe a new client
        // I can use this.Clients.Caller but this is demo purpose
        tradingCoordinator.Subscribe(connId, 1000., this.Clients) 
        base.OnConnected()

    override x.OnDisconnected(stopCalled) =
        ignore <| System.Threading.Interlocked.Decrement(userCount)
        let connId = x.Context.ConnectionId
        logUserEvent "OnDisconnected" connId

        // Unsubscribe client
        tradingCoordinator.Unsubscribe(connId)
        base.OnDisconnected(stopCalled)

    member x.GetAllStocks() =
        let connId = x.Context.ConnectionId
        logUserCall connId "GetAllStocks"

        let stocks = stockMarket.GetAllStocks(connId)
        for stock in stocks do
            this.Clients.Caller.SetStock stock

    member x.OpenMarket() =
        let connId = x.Context.ConnectionId
        logUserCall connId "OpenMarket"

        stockMarket.OpenMarket(connId)
        this.Clients.All.SetMarketState(MarketState.Open.ToString())

    member x.CloseMarket() =
        let connId = x.Context.ConnectionId
        logUserCall connId "CloseMarket"

        stockMarket.CloseMarket(connId)
        this.Clients.All.SetMarketState(MarketState.Closed.ToString())

    member x.GetMarketState() =
        let connId = x.Context.ConnectionId
        logUserCall connId "GetMarketState"

        let state = stockMarket.GetMarketState(connId)
        state.ToString()
