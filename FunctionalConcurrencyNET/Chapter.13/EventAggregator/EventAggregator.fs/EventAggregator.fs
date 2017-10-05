namespace EventAggregator

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Concurrency
open System.Threading
open System.Threading

type IEventAggregator =
    inherit IDisposable
    abstract GetEvent<'Event> : unit -> IObservable<'Event>
    abstract Publish<'Event> : eventToPublish:'Event -> unit

type internal EventAggregator() =

    let disposedErrorMessage = "The EventAggregator is already disposed."
    let subject = new Subject<obj>()

    interface IEventAggregator with
        member this.GetEvent<'Event>(): IObservable<'Event> =

            if (subject.IsDisposed) then
                failwith disposedErrorMessage
            subject.OfType<'Event>().AsObservable<'Event>().SubscribeOn(TaskPoolScheduler.Default)

        member this.Publish(eventToPublish: 'Event): unit =
            if (subject.IsDisposed) then
                failwith disposedErrorMessage
            subject.OnNext(eventToPublish)

        member this.Dispose(): unit = subject.Dispose()

    static member Create() = new EventAggregator() :> IEventAggregator



