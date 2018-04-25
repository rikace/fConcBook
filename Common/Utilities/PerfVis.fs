module PerfVis

open FSharp.Charting
open PerfUtil

type PerfTestInput  = (string * ((unit->unit) list)) list
type PerfTestOutput = (string * (PerfResult list)) list

let runOnce implementaions =
    implementaions
    |> List.map (fun (name, impl) ->
        name, [impl])

let private execute (perfTestInput:PerfTestInput):PerfTestOutput=
    perfTestInput
    |> List.map (fun (name, implementations) ->
        printfn "---------------------------------"
        printfn "Running '%s' implementation ..." name
        let perfResults =
            implementations
            |> List.mapi (fun i impl ->
                printfn "----Executing attempt #%d" i
                let perfResult = Benchmark.Run impl
                printfn "PerfResult:%A\n" perfResult
                perfResult
            )
        name, perfResults
    )

let private buildChart includeGCgeneration title (perfResults:PerfTestOutput) =
    let getAverageTimeBy selector =
        perfResults |> List.map (fun (name, perfResults) ->
            let totalTime = perfResults |> List.sumBy selector
            name, totalTime/float(perfResults.Length))

    let elapsedTimeData = getAverageTimeBy (fun x -> x.Elapsed.TotalMilliseconds)
    let cpuTimeData     = getAverageTimeBy (fun x -> x.CpuTime.TotalMilliseconds)
    let getLabels       = List.map (snd >> sprintf "%.3f")

    let addGcGeneration displayGCflag charts =
        if displayGCflag then
            let getGcAverageTimeBy n = getAverageTimeBy (fun x -> x.GcDelta.Item n |> float), sprintf "GC %d Gen" n
            let gcColumns =
                [   getGcAverageTimeBy 0
                    getGcAverageTimeBy 1
                    getGcAverageTimeBy 2 ]
                |> List.filter(fun (gcInfo,_) -> gcInfo |> List.filter(fun (_,gcGen) -> gcGen > 0.) |> (List.isEmpty >> not))
                |> List.map(fun (gcInfo,label) -> Chart.Column(gcInfo,     Name=label,     Labels=getLabels gcInfo))
            charts @ gcColumns
        else charts

    [   Chart.Column(elapsedTimeData, Name="Elapsed Time (ms)", Labels=(getLabels elapsedTimeData))
        Chart.Column(cpuTimeData,     Name="CPU Time (ms)",     Labels=(getLabels cpuTimeData))
    ]
    |> addGcGeneration includeGCgeneration
    |> Chart.Combine
    |> Chart.WithLegend(Title="Elapsed vs CPU Time (ms)")
    |> Chart.WithTitle(Text=title)

let private createChart includeGCgeneration title (perfResults:PerfTestOutput) =
    let chart = buildChart includeGCgeneration title perfResults
    chart.ShowChart()

let toChart title =
    execute >> (createChart false title)

let toChartWithGCgeneration title =
    execute >> (createChart true title)

let fromTuples (input:System.Tuple<string,System.Action[]>[]):PerfTestInput =
    input
    |> Array.map (fun (tuple : System.Tuple<string, System.Action[]>) ->
        let (name, actions) = (tuple : System.Tuple<string, System.Action[]>)
        let implementations =
            actions
            |> Array.map (fun action ->
                fun () -> action.Invoke())
            |> Array.toList
        name, implementations)
    |> Array.toList

let CreateLineChart values keys title =
    let data = Seq.zip keys values
    Chart.Line(data, title)
let CombineToForm charts title =
    let chart =
        Chart.Combine charts
        |> Chart.WithLegend(true)
        |> Chart.WithTitle(title)
    chart.ShowChart()

let Run (action:System.Action) =
    Benchmark.Run (action.Invoke)

let CombineAndShowGcCharts (titles:string[]) (perfResults:PerfResult[]) =
    let chart =
        Array.zip titles perfResults
        |> Array.map (fun (t,p) ->
            let data = p.GcDelta |> List.mapi (fun i x -> (sprintf "GcDelta[%d]" i), x)
            let labels = p.GcDelta |> List.map (sprintf "%d")
            Chart.Column(data, Name=t, Labels = labels)
            )
        |> Chart.Combine
        |> Chart.WithTitle "GC usage"
        |> Chart.WithLegend (true)
    chart.ShowChart()