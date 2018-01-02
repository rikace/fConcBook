open System
open System.Windows.Forms

[<EntryPoint>]
let main argv =

    let rand = Random(int(DateTime.Now.Ticks))
    let attempts = 5
    let dataSamples =
        List.init attempts (fun _ ->
            List.init 1000000 (fun i -> rand.Next()))
    let run sort =
        dataSamples
        |> List.map (fun data ->
            fun () -> sort data |> ignore
        )

    [
        "Sequential"        , run QuickSort.quicksortSequential
        "Parallel"          , run QuickSort.quicksortParallel
        "ParallelWithDepth" , run (QuickSort.quicksortParallelWithDepth QuickSort.ParallelismHelpers.MaxDepth)
    ]
    |> PerfVis.toChart "F# QuickSort"
    |> Application.Run

    0
