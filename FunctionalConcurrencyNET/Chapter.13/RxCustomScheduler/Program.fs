module Main

open System
open System.Reactive.Linq
open RxIScheduler
open System.Reactive.Concurrency
open System.Threading

[<EntryPoint>]
let main argv =
    let scheduler = ParallelAgentScheduler.Create(4)

    Observable.Interval(TimeSpan.FromSeconds(0.4))
        .SubscribeOn(scheduler)
        .Subscribe(fun _ ->
            printfn "ThreadId: %A " Thread.CurrentThread.ManagedThreadId
        )
    |> ignore

    Console.ReadLine() |> ignore
    0