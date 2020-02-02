namespace StockTicker.Server.FSharp

module StockTickerHub = 

    open System.Threading.Tasks
    open Microsoft.AspNetCore.SignalR
    open StockTicker.Core
    open StockTicker.Core.Logging
    open StockTicker.Core.Logging.Message
    open StockTicker.Server.FSharp
    open TradingCoordinator
    open StockMarket     
    
    let logger = Log.create "StockTicker.Hub"

    // Listing 14.10 Stock Ticker SignalR hub
    // Hub with "Stock Market" operations
    // It is responsible to update the stock-ticker
    // to each client registered    
    type StockTicker() as this =
        inherit Hub()  // #B
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

        override this.OnConnectedAsync() = // #D
            ignore <| System.Threading.Interlocked.Increment(userCount)
            let connId = this.Context.ConnectionId
            logUserEvent "OnConnected" connId            
            base.OnConnectedAsync()
            
        member this.Subscribe(userName:string, initialCash:decimal) =
            tradingCoordinator.Subscribe(this.Context.ConnectionId, userName, initialCash, this.Clients.Caller) // #E
            |> Task.FromResult
            :> Task
            
        override this.OnDisconnectedAsync(stopCalled) =  // #D
            ignore <| System.Threading.Interlocked.Decrement(userCount)
            let connId = this.Context.ConnectionId
            logUserEvent "OnDisconnected" connId

            // un-subscribe client
            tradingCoordinator.Unsubscribe(connId) // #E
            base.OnDisconnectedAsync(stopCalled)

        member this.GetAllStocks() = // #F
            let connId = this.Context.ConnectionId
            logUserCall connId "GetAllStocks"

            let stocks = stockMarket.GetAllStocks(connId)
            this.Clients.Caller.SendAsync("setStocks", stocks |> Seq.toArray)

        member this.OpenMarket() = // #F
            let connId = this.Context.ConnectionId
            logUserCall connId "OpenMarket"
            stockMarket.OpenMarket(connId)
            
        member this.CloseMarket() = // #F
            let connId = this.Context.ConnectionId
            logUserCall connId "CloseMarket"

            stockMarket.CloseMarket(connId)

        member this.GetMarketState() = // #F
            async {
                let connId = this.Context.ConnectionId
                logUserCall connId "GetMarketState"            
                let! marketState = stockMarket.GetMarketState(connId)
                return marketState.ToString()
            } |> Async.StartAsTask
            
            
        interface IStockTickerHubClient with
            member __.GetMarketState () = this.GetMarketState()            
            member __.CloseMarket () = this.CloseMarket()
            member __.OpenMarket () = this.OpenMarket()
            member __.GetAllStocks () = this.GetAllStocks()
            member __.Subscribe (userName:string, initialCash:decimal) = this.Subscribe(userName, initialCash)
            