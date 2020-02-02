namespace StockTicker.Core

open System
open StockTicker.Core

// The verbs of the system (in imperfect form)
module Events =

    // Events implemented as discriminated union.
    // If you use a big solution, change to a base type
    // or just use many event storages and concatenate / merge them
    type Event =
        | StocksBoughtEvent of Guid * ClientOrder
        | StocksSoldEvent of Guid * ClientOrder
        | ErrorSubmittingOrder of Guid * ClientOrder * exn

        override this.ToString() =
            match this with

            | StocksBoughtEvent(id, trading) ->
                    sprintf "Item Id %A - Ticker %s sold at $ %f - quantity %d" id trading.Symbol trading.Price trading.Quantity

            | StocksSoldEvent(id, trading) ->
                    sprintf "Item Id %A - Ticker %s bought at $ %f - quantity %d" id trading.Symbol trading.Price trading.Quantity

            | ErrorSubmittingOrder(id, trading, e) ->
                   sprintf "Item Id %A - Ticker %s - Error Message : %s" id trading.Symbol e.Message

        static member Create (id:Guid, eventData:Event) = EventDescriptor(id, eventData)

    // Container to encapsulate events
    and EventDescriptor(id:Guid, eventData:Event) =
        member this.Id = id
        member this.EventData = eventData

