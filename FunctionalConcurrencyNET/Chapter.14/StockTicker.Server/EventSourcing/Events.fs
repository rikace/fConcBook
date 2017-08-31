namespace StockTicker.Events

open System
open System.Threading.Tasks
open System.Collections.Generic
open StockTicker.Core


// The verbs of the system (in imperfect form)
module Events =

    // Events implemented as discriminated union.
    // If you use a big solution, change to a base type
    // or just use many event storages and concatenate / merge them
    type Event =
        | StocksBuyedEvent of Guid * TradingRecord
        | StocksSoldEvent of Guid * TradingRecord
        | ErrorSubmitingOrder of Guid * TradingRecord * exn

        override this.ToString() =
            match this with

            | StocksBuyedEvent(id, trading) ->
                    sprintf "Item Id %A - Ticker %s sold at $ %f - quantity %d" id trading.Symbol trading.Price trading.Quantity

            | StocksSoldEvent(id, trading) ->
                    sprintf "Item Id %A - Ticker %s bought at $ %f - quantity %d" id trading.Symbol trading.Price trading.Quantity

            | ErrorSubmitingOrder(id, trading, e) ->
                   sprintf "Item Id %A - Ticker %s - Error Message : %s" id trading.Symbol e.Message

        static member Create (id:Guid, eventData:Event) = EventDescriptor(id, eventData)

    // Container to encapsulate events
    and EventDescriptor(id:Guid, eventData:Event) =
        member this.Id = id
        member this.EventData = eventData

