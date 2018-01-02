module KMeans.Visualizer

open KMeans.Data
open FSharp.Charting
open Accord.Statistics.Analysis

let toPairs (data:float[][]) =
    data |> Array.map (fun x->x.[0], x.[1])

let pca, points =
    let model =
        PrincipalComponentAnalysis(
            Method = PrincipalComponentMethod.Center,
            Whiten = true)
    model.Learn(data) |> ignore
    model.NumberOfOutputs <- 2
    model, model.Transform(data) |> toPairs


let plotCentorids i (centroids: float[][]) =
    let centroidPoints = pca.Transform(centroids) |> toPairs
    Chart.Combine(
      [Chart.Point(points, MarkerSize=4, Name=sprintf "Data points #%d" i)
       Chart.Point(centroidPoints, MarkerSize=8, Name=sprintf "Centroids #%d" i,
                    Color=System.Drawing.Color.Red)
      ])
