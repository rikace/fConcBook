namespace StockTicker.Commands

open System
open System.Collections.Generic
open StockTicker.Events
open EventStorage
open StockTicker.Core
open System.Threading.Tasks
open StockTicker
open StockMarket
open Events

// Listing 14.4 Command handler with async retry logic
module CommandHandler =
    // Listing 9.4 Retry Async Builder
    let private retryPublish = RetryAsyncBuilder(10, 250) // #A

    let tradingCoordinator = TradingCoordinator.Instance()   // #B
    let Storage = new EventStorage()     // #C

    let AsyncHandle (commandWrapper:CommandWrapper) =   // #D
        let connectionId = commandWrapper.ConnectionId

        retryPublish {      // #A
            tradingCoordinator.PublishCommand(PublishCommand(connectionId, commandWrapper))   // #E

            let event =
                let cmd = commandWrapper.Command
                match cmd with     // #F
                | BuyStockCommand(connId,trading) -> StocksBuyedEvent(commandWrapper.Id, trading)
                | SellStockCommand(connId, trading) -> StocksSoldEvent(commandWrapper.Id, trading)   // #F

            let eventDescriptor = Event.Create (commandWrapper.Id, event)
            Storage.SaveEvent (Guid(connectionId)) eventDescriptor    // #G
        }