namespace DataParallelism.Part1.FSharp

module Benchmark =

    open BenchmarkDotNet.Attributes

    [<MemoryDiagnoser>]
    [<RPlotExporter; RankColumn>]
    type BenchmarkPrimeSum() =

        [<Params(1_000_000, 10_000_000, 100_000_000)>]
        [<DefaultValue>]
        val mutable N : int
        
        [<DefaultValue>]
        val mutable len : int
        
        [<GlobalSetup>]
        member this.Setup() =
            this.len <- this.N
            
        [<Benchmark>]    
        member this.Sequential() = PrimeNumbers.sequentialSum this.len   

        [<Benchmark>]    
        member this.Parallel() = PrimeNumbers.parallelSum this.len
        
        [<Benchmark>]    
        member this.ParallelStruct() = PrimeNumbers.parallelLinqSum this.len
        
        
    [<MemoryDiagnoser>]
    [<RPlotExporter; RankColumn>]
    type BenchmarkMandelbrot() =

        [<Params(1000, 2000, 3000)>]
        [<DefaultValue>]
        val mutable N : int
        
        [<DefaultValue>]
        val mutable size : int
        
        [<GlobalSetup>]
        member this.Setup() =
            this.size <- this.N
            
        [<Benchmark>]    
        member this.Sequential() = Mandelbrot.sequentialMandelbrot this.size   

        [<Benchmark>]    
        member this.Parallel() = Mandelbrot.parallelMandelbrot this.size    
        
        [<Benchmark>]    
        member this.ParallelStruct() = Mandelbrot.parallelMandelbrotStruct this.size    