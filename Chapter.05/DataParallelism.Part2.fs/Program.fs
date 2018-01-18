open System
open System.Linq
open System.Windows.Forms
open FSharp.Collections.ParallelSeq
open DataParallelism.Part2.CSharp
open MapReduce
open KMeans
open KMeans.Data
open FSharp.Charting

// KMeans clustering
let kMeansDemo() =
    let M = 7 // number of experiments / random initial centroids
    let initialCentroidsSet =
        [ for i in [1..M] do
            yield data |> getRandomCentroids 11 ]

    let run func =
        initialCentroidsSet
        |> List.map (fun initialCentroids ->
            fun () -> func data dist initialCentroids |> ignore)

    let methods =
        [
        "C# LINQ",              (fun data _ initialCentroids -> KMeansLinq(data).Run(initialCentroids))
        "C# PLINQ", (fun data _ initialCentroids -> KMeansPLinq(data).Run(initialCentroids))
        "C# PLINQ Partitioner", (fun data _ initialCentroids -> KMeans(data).Run(initialCentroids))
        "F# PSeq",              (KMeans.FsPSeq.kmeans)
        ]

    // Compare performance of k-means implementations
    methods
    |> List.map (fun (name, f) -> (name, run f))
    |> PerfVis.toChart "KMeans Clustering"
    |> Application.Run

    // Plot data set and centroids
    printfn "Computing results ..."
    let plot (initialCentroids:float[][]) =
        methods
        |> List.mapi (fun i (name,f) ->
            printfn "> Running '%s' implementation ..." name
            f data dist initialCentroids
            |> Visualizer.plotCentorids i
            |> Chart.WithLegend(Title=name)
        )
        |> List.chunkBySize 3
        |> List.map (Chart.Columns)
        |> Chart.Rows
    let chart = plot (initialCentroidsSet.[1])
    Application.Run(chart.ShowChart())



// PageRank map-reduce demo
let pageRankDemo() =

    let data = Data.loadPackages()
    printfn "Number of packages : %d" data.Length
    printfn "%d" (data |> Seq.sumBy(fun x->x.Dependencies.Length))

    let M,R = 10,5
    let fsWrapper f =
        fun (ranks:(string*float)seq) ->
            let pg = Task.PageRank(ranks)
            f data (pg.Map) (pg.Reduce) M R
            |> Seq.ofList
    let csWrapper f =
        fun (ranks:(string*float)seq) ->
            let pg = Task.PageRank(ranks)
            let reduce (g:IGrouping<string, string*float>) =
                let key = g.Key
                let values = g |> Seq.map (snd)
                pg.Reduce key values
            f(data, pg.Map, fst, reduce, M, R)
            |> Seq.ofArray

    // Warm-up (6-7sec)
    Demo.benchmark "Warm-up" (fun () ->
        data
        |> PSeq.withDegreeOfParallelism M
        |> PSeq.groupBy (id)
        |> PSeq.toList
        |> ignore
    )

    let execute N func =
        let rec loop N (ranks:(string*float)seq) =
            printfn "\tIterations left: %d" N
            if N = 0 then ranks
            else func ranks |> loop (N-1)
        loop N []

    let run func = [fun () -> execute 10 func |> ignore]
    [
        "LINQ",                 (run (fsWrapper <| MapReduceSequential.mapReduce))
        "C# PLINQ",             (run (csWrapper <| MapReducePLINQ.MapReduce))
        "F# PSeq",              (run (fsWrapper <| MapReduceFsPSeq.mapReduce))
        "C# PLINQ Partitioner", (run (csWrapper <| MapReducePLINQPartitioner.MapReduce))
    ]
    |> PerfVis.toChart "" //NuGet PageRank"
    |> Application.Run

    // Do more iteration
    let ranks =
        execute 100 (csWrapper <| MapReducePLINQPartitioner.MapReduce)

    printfn "Most important packages:"
    ranks
    |> Seq.sortByDescending (snd)
    |> Seq.take 10
    |> Seq.iter (fun (name, score) ->
        printfn "\t%s : %f" name score)


[<EntryPoint>]
[<STAThread>]
let main argv =

    Demo.printSeparator()
    printfn "KMeans clustering"
    kMeansDemo()

    Demo.benchmark
        "Listing 5.13 Parallel sum of prime numbers in a collection using the F# Array.Parallel module"
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
            printfn "Prime sum %d" primeSum)


    Demo.printSeparator()
    printfn "5.3.2 MapReduce the NuGet package gallery"
    pageRankDemo()

    0 // return an integer exit code