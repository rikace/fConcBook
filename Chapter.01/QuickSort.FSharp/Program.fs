open System
open QuickSort.FSharp
open BenchmarkDotNet.Running
open BenchmarkUtils

[<EntryPoint>]
let main argv =
    
    let performanceStats = BenchmarkRunner.Run<Benchmark.BenchmarkQuickSort>()    
    let summary = Charting.mapSummary performanceStats
    
    Charting.drawSummaryReport summary
    
    Console.ReadLine() |> ignore
    
    0
