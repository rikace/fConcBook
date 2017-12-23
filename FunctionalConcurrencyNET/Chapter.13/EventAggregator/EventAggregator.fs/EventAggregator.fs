namespace EventAggregator

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Concurrency
open System.Threading.Tasks

// Listing 13.6  Event Aggregator using Reactive Extension
type IEventAggregator = // #A
    inherit IDisposable
    abstract GetEvent<'Event> : unit -> IObservable<'Event>
    abstract Publish<'Event> : eventToPublish:'Event -> unit

type internal EventAggregator() =
    let disposedErrorMessage = "The EventAggregator is already disposed."

    let subject = new Subject<obj>() // #B

    interface IEventAggregator with
        member this.GetEvent<'Event>(): IObservable<'Event> =
            if (subject.IsDisposed) then
                failwith disposedErrorMessage
            subject.OfType<'Event>().AsObservable<'Event>() // #C
                .SubscribeOn(TaskPoolScheduler.Default)

        member this.Publish(eventToPublish: 'Event): unit =
            if (subject.IsDisposed) then
                failwith disposedErrorMessage
            subject.OnNext(eventToPublish) // #D

        member this.Dispose(): unit = subject.Dispose()

    static member Create() = new EventAggregator() :> IEventAggregator // #E



