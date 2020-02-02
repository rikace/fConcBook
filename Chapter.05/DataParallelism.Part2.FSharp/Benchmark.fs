namespace DataParallelism.Part2.FSharp

module Benchmark =

    open System.Linq
    open BenchmarkDotNet.Attributes
    open DataParallelism.Part2.CSharp

    [<MemoryDiagnoser>]
    [<RPlotExporter; RankColumn>]
    type BenchmarkKMeans() =

        [<Params(10, 11, 12, 13, 15)>]
        [<DefaultValue>]
        val mutable N : int
        
        [<DefaultValue>]
        val mutable data : float[][]
        
        [<DefaultValue>]
        val mutable initialCentroids : float[][]
        
        [<GlobalSetup>]
        member this.Setup() =
            this.data <- KMeans.Data.data
            this.initialCentroids <- KMeans.Data.data |> KMeans.Data.getRandomCentroids this.N
          
            
        [<Benchmark>]    
        member this.Sequential() =  KMeansLinq(this.data).Run(this.initialCentroids)

        [<Benchmark>]    
        member this.ParallelLINQ() =  KMeansPLinq(this.data).Run(this.initialCentroids)
        
        [<Benchmark>]    
        member this.PLINQPartitioner() = KMeansPLinqPartition(this.data).Run(this.initialCentroids)

        [<Benchmark>]    
        member this.PSeq() = KMeans.FsPSeq.kmeans this.data KMeans.Data.dist this.initialCentroids



    [<MemoryDiagnoser>]
    [<RPlotExporter; RankColumn>]
    type BenchmarkMapReduce() =
            
        let data = MapReduce.Data.loadPackages()

        let M,R = 10,5
        let fsWrapper f =
            fun (ranks:(string*float)seq) ->
                let pg = MapReduce.Task.PageRank(ranks)
                f data (pg.Map) (pg.Reduce) M R
                |> Seq.ofList
        let csWrapper f =
            fun (ranks:(string*float)seq) ->
                let pg = MapReduce.Task.PageRank(ranks)
                let reduce (g:IGrouping<string, string*float>) =
                    let key = g.Key
                    let values = g |> Seq.map (snd)
                    pg.Reduce key values
                f(data, pg.Map, fst, reduce, M, R)
                |> Seq.ofArray
                
        let execute N func =
            let rec loop N (ranks:(string*float)seq) =
                if N = 0 then ranks
                else func ranks |> loop (N-1)
            loop N []                
            
        [<Benchmark>]    
        member this.Sequential() = execute 10 (fsWrapper MapReduce.MapReduceSequential.mapReduce) 

        [<Benchmark>]    
        member this.ParallelLINQ() = execute 10 (csWrapper MapReducePLINQ.MapReduce) 
      
        [<Benchmark>]    
        member this.PSeq() = execute 10 (fsWrapper MapReduce.FsPSeq.mapReduce) 
  
        [<Benchmark>]    
        member this.PLINQPartitioner() = execute 10 (csWrapper MapReducePLINQPartitioner.MapReduce)