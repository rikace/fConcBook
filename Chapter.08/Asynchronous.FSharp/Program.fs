open System
open BenchmarkUtils.Benchmark
open Asynchronous.FSharp

[<EntryPoint>]
let main argv =
    Bench.Time("Async impl", (fun () ->
        AsyncModule.runAsync() |> ignore))
    
    Bench.Time("Sync impl", (fun () ->
        AsyncModule.runSync() |> ignore))
    
    Console.ReadLine() |> ignore
    0
    
    