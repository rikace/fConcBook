open System

open BenchmarkDotNet.Running
open BenchmarkUtils
open DataParallelism.Part1.FSharp

[<EntryPoint>]
let main argv =

    let runBenchmarkPrimeNumberSum () = 
        let performanceStats = BenchmarkRunner.Run<Benchmark.BenchmarkMandelbrot>()
        let summary = Charting.mapSummary(performanceStats)
        Charting.drawSummaryReport(summary)
        
    let runBenchmarkMandelbrot() =
        let performanceStats = BenchmarkRunner.Run<Benchmark.BenchmarkPrimeSum>()
        let summary = Charting.mapSummary(performanceStats)
        Charting.drawSummaryReport(summary)
        
    runBenchmarkPrimeNumberSum()
    Console.ReadLine() |> ignore
    
    runBenchmarkMandelbrot()
    Console.ReadLine() |> ignore

    0
