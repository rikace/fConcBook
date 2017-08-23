module KMeans.Data

open System
open System.IO

let [<Literal>] DataSet =
    "http://archive.ics.uci.edu/ml/machine-learning-databases/wine-quality/winequality-white.csv"
let [<Literal>] LocalCopyFilePath=
    __SOURCE_DIRECTORY__ + "/winequality-white.csv"

let getDataSet () =
    if File.Exists LocalCopyFilePath
    then
        printfn "Loading local copy of `winequality-white.csv` ..."
        File.ReadAllText LocalCopyFilePath
    else
        printfn "Downloading `winequality-white.csv` from Internet ..."
        use client = new System.Net.WebClient()
        client.DownloadString(DataSet)

let classes, data =
    getDataSet()
        .Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)
    |> Array.skip 1
    |> Array.map (fun line ->
        let x = line.Split(';') |> Array.map (float)
        let _class = int(x.[x.Length-1])
        let features = Array.sub x 0 (x.Length-1)
        _class, features)
    |> Array.unzip

let dist:(float[] -> float[] -> float) =
    Array.fold2 (fun x u v -> x + pown (u - v) 2) 0.0

let classesError =
    Seq.zip classes data
    |> Seq.groupBy (fst)
    |> Seq.sumBy (fun (_, points) ->
        let centroid =
            Array.init (data.[0].Length) (fun i ->
                points |> Seq.averageBy (fun (_,x)-> x.[i]))
        points
        |> Seq.sumBy (fun (_,u) -> dist u centroid)
    )

let getRandomCentroids =
    let seed = (int) DateTime.Now.Ticks
    let rnd = System.Random(seed)
    (fun k (data:float[][]) ->
        Array.init k (fun _ -> data.[rnd.Next(data.Length)]))


let getError centroids =
    let nearestCentroid u =
        Array.minBy (dist u) centroids
    data
    |> Seq.groupBy (nearestCentroid)
    |> Seq.sumBy (fun (centroid, points) ->
        points |> Seq.sumBy (dist centroid)
    )
