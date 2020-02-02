namespace StockTicker.Server.FSharp.Bus

open System
open PublishWithRetry
open StockTicker.Server.FSharp
open TradingCoordinator
open StockTicker.Core
open Events
open EventStorage

module private Retry =
    // Listing 9.4 Retry Async Builder
    let retryPublish = RetryAsyncBuilder(10, 250) // #A

// Listing 14.4 Command handler with async retry logic
module CommandHandler =
    open Retry
    
    let tradingCoordinator = TradingCoordinator.Instance()   // #B
    
    let AsyncHandle (commandWrapper:CommandWrapper) =   // #D
        let connectionId = commandWrapper.ConnectionId

        retryPublish {      // #A
            tradingCoordinator.PublishCommand(CoordinatorMessage.PublishCommand(connectionId, commandWrapper))   // #E
    }
        
// Listing 14.4 Command handler with async retry logic
module EventStorageHandler =
    open Retry
    let Storage = new EventStorage()     // #C

    let AsyncHandle (commandWrapper:CommandWrapper) =   // #D
        let connectionId = commandWrapper.ConnectionId

        retryPublish {      // #A
            let event =
                let cmd = commandWrapper.Command
                match cmd with     // #F
                | BuyStockCommand(connId,trading) -> StocksBoughtEvent(commandWrapper.Id, trading)
                | SellStockCommand(connId, trading) -> StocksSoldEvent(commandWrapper.Id, trading)   // #F

            let eventDescriptor = Event.Create (commandWrapper.Id, event)
            Storage.SaveEvent (Guid(connectionId), eventDescriptor)    // #G
        }        