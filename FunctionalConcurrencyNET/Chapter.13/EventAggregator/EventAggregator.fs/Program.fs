module Program

open System
open EventAggregator
open System.Threading
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency

type IncrementEvent = { Value: int }
type ResetEvent = { ResetTime: DateTime }

[<EntryPoint>]
let main argv =

    use evtAggregator = EventAggregator.Create()

    let disposeResetEvent =
        evtAggregator.GetEvent<ResetEvent>()
            .ObserveOn(Scheduler.CurrentThread)
            .Subscribe(fun evt ->
                printfn "Counter Reset at: %A - Thread Id %d" evt.ResetTime Thread.CurrentThread.ManagedThreadId)

    let disposeIncrementEvent =
        evtAggregator.GetEvent<IncrementEvent>()
            .ObserveOn(Scheduler.CurrentThread)
            .Subscribe(fun evt ->
                printfn "Counter Incremented. Value: %d - Thread Id %d" evt.Value Thread.CurrentThread.ManagedThreadId)

    for i in [0..10] do
        evtAggregator.Publish({ Value = i })

    evtAggregator.Publish({ ResetTime = DateTime(2015, 10, 21) })

    Console.ReadLine() |> ignore
    0