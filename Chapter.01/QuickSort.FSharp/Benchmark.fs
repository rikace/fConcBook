namespace QuickSort.FSharp

module Benchmark =

    open System
    open BenchmarkDotNet.Attributes
 
    [<MemoryDiagnoser>]
    [<RPlotExporter; RankColumn>]
    type BenchmarkQuickSort() =

        [<Params(1000, 10000)>]
        [<DefaultValue>]
        val mutable N : int
        
        [<DefaultValue>]
        val mutable iterations : int[]
        
        [<GlobalSetup>]
        member this.Setup() =
            let rand = new Random((int) DateTime.Now.Ticks)
            this.iterations <- Array.init this.N (fun _ -> rand.Next())
            
        [<Benchmark>]    
        member this.Sequential() = Functions.quicksortSequential (this.iterations |> Array.toList)    

        [<Benchmark>]    
        member this.Parallel() = Functions.quicksortParallel (this.iterations |> Array.toList)    
        
        [<Benchmark>]    
        member this.ParallelDepth() = Functions.quicksortParallelWithDepth Functions.ParallelismHelpers.MaxDepth (this.iterations |> Array.toList)