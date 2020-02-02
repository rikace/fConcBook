open System
open DataParallelism.Part2.FSharp
open BenchmarkDotNet.Running
open BenchmarkUtils
open Utilities

[<EntryPoint>]
[<STAThread>]
let main argv =
    
    let runBenchmarkKMeans() =
        let performanceStats = BenchmarkRunner.Run<Benchmark.BenchmarkKMeans>()
        let summary = Charting.mapSummary(performanceStats)
        Charting.drawSummaryReport(summary)

    let runBenchmarkMapReduce() =
        let performanceStats = BenchmarkRunner.Run<Benchmark.BenchmarkMapReduce>()
        let summary = Charting.mapSummary(performanceStats)
        Charting.drawSummaryReport(summary)

    
    Demo.printSeparator()
    printfn "KMeans clustering"
    runBenchmarkKMeans()
    
    Demo.printSeparator()
    
    Benchmark.Bench.Time(
        "Listing 5.13 Parallel sum of prime numbers in a collection using the F# Array.Parallel module",
        (fun () ->
            let isPrime n = //#A
                if n = 1 then false
                elif n = 2 then true
                else
                    let boundary = int (Math.Floor(Math.Sqrt(float(n))))
                    [2..boundary - 1]
                    |> Seq.forall(fun i -> n % i <> 0)
            let primeSum =
                [|0..1000000|]
                |> Array.Parallel.choose (fun x-> //#B
                    if isPrime x then Some(int64(x)) else None)
                |> Array.sum
            printfn "Prime sum %d" primeSum))


    Demo.printSeparator()
    printfn "5.3.2 MapReduce the NuGet package gallery"
    runBenchmarkMapReduce()

    0