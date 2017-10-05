open System
open EventAggregator
open System.Threading

type IncrementEvent = { Value: int }
type ResetEvent = { ResetTime: DateTime }

[<EntryPoint>]
let main argv =

    let evtAggregator = EventAggregator.Create()

    let disposeResetEvent =
        evtAggregator.GetEvent<ResetEvent>().Subscribe(fun evt -> printfn "Counter Reset at: %A - Thread Id %d" evt.ResetTime Thread.CurrentThread.ManagedThreadId)


    let disposeIncrementEvent =
        evtAggregator.GetEvent<IncrementEvent>().Subscribe(fun evt ->  printfn "Counter Incremented. Value: %d - Thread Id %d" evt.Value Thread.CurrentThread.ManagedThreadId)

    for i in [0..10] do
        evtAggregator.Publish({ Value = i })

    evtAggregator.Publish({ ResetTime = DateTime.Now })

    Console.ReadLine() |> ignore
    0