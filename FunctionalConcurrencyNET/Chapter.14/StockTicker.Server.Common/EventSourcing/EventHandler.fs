namespace StockTicker.Events

open System
open System.Collections.Generic
open Events

// Listing 14.5 EventBus implementation using Agent
/// Event broker for event based communication
[<AutoOpen>]
module EventBus =
    /// Used just to notify others if anyone would be interested
    let public EventPublisher = new Event<Event>()  // #A

    /// Used to subscribe to event changes
    let public Subscribe (eventHandle: Events.Event -> unit) =
        EventPublisher.Publish |> Observable.subscribe(eventHandle) // #A

    let public Notify (event:Event) = EventPublisher.Trigger event // #A


module EventStorage =

    type EventStorageMessage =     // #B
        | SaveEvent of id:Guid * event:EventDescriptor
        | GetEventsHistory of Guid * AsyncReplyChannel<Event list option>

    /// Custom implementation of in-memory time async event storage. Using message passing.
    type EventStorage() =     // #C
        let eventstorage = MailboxProcessor.Start(fun inbox ->
            let rec loop (history:Dictionary<Guid, EventDescriptor list>) = async { // #D
                let! msg = inbox.Receive()
                match msg with
                | SaveEvent(id, event) ->  // #E
                    EventBus.Notify event.EventData  // #A

                    match history.TryGetValue(id) with
                    | true, events -> history.[id] <- (event :: events)
                    | false, _ -> history.Add(id, [event])

                | GetEventsHistory(id, reply) ->   // #F
                    match history.TryGetValue(id) with
                    | true, events ->
                        events |> List.map (fun i -> i.EventData) |> Some
                        |> reply.Reply
                    | false, _ -> reply.Reply(None)
                return! loop history
            }
            loop (Dictionary<Guid, EventDescriptor list>(HashIdentity.Structural))) // #D

        member this.SaveEvent(id:Guid) (event:EventDescriptor) =  // #E
            eventstorage.Post(SaveEvent(id, event))

        member this.GetEventsHistory(id:Guid) =   // #F
            eventstorage.PostAndReply(fun rep -> GetEventsHistory(id,rep))
            |> Option.map(List.rev) // #G