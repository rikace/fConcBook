namespace BenchmarkUtils

open System
open BenchmarkDotNet.Running
open BenchmarkDotNet.Reports
open XPlot.GoogleCharts

type GcMeasures = {gen0Collections: int;gen1Collections: int;gen2Collections: int }// allocatedBytes: int64 }
type Metrics = {case: string; N : int; param : string; median: float; mean : float; gcStats: GcMeasures}
type Measurement = {case: string; metrics : Metrics list}
type Report =  {title:string; totalTime:TimeSpan; measurements : Measurement list }

module Charting =
    [<CompiledName("MapSummary")>]
    let mapSummary (summary : Summary) =
        let measures =
            summary.BenchmarksCases
            |> Seq.zip summary.Reports
            |> Seq.map(fun ((p : BenchmarkReport), (c : BenchmarkCase)) ->
                { Metrics.case = c.Descriptor.DisplayInfo
                  Metrics.N = p.ResultStatistics.N
                  Metrics.param = c.Parameters.ValueInfo
                  Metrics.median = p.ResultStatistics.Median
                  Metrics.mean = p.ResultStatistics.Mean
                  Metrics.gcStats = {
                      GcMeasures.gen0Collections = p.GcStats.Gen0Collections
                      GcMeasures.gen1Collections = p.GcStats.Gen1Collections
                      GcMeasures.gen2Collections = p.GcStats.Gen2Collections
                  }
                })
            |> Seq.groupBy(fun f -> f.case)
            |> Seq.map(fun c ->
                {
                    Measurement.case = fst c
                    Measurement.metrics = snd c |> Seq.toList
                })
            |> Seq.toList
        { Report.title = summary.Title
          Report.totalTime = summary.TotalTime
          Report.measurements = measures }
    
    [<CompiledName("GetGCGenerationsReport")>]
    let getGCgenerationsReport (gdMeasures : Measurement list) = [
        for c in gdMeasures do
              yield[ for m in c.metrics do
                        yield (sprintf "%s-GC 0" m.param, float m.gcStats.gen0Collections)
                        yield (sprintf "%s-GC 1" m.param, float m.gcStats.gen1Collections) 
                        yield (sprintf "%s-GC 2" m.param, float m.gcStats.gen2Collections) ]
        ]
    
    [<CompiledName("DrawSummaryReport")>]
    let drawSummaryReport (report : Report) =
        let reportInputs = [
            for c in report.measurements do
                 yield [ for m in c.metrics -> (m.param, m.mean)]
            ]
        let labels : string list = [
            for c in report.measurements do
                yield c.case.Substring(max 0 (c.case.LastIndexOf(".") + 1)) ]
        let options =
            Options(title = report.title, hAxis = Axis(logScale = true))
        
        reportInputs
        |> Chart.Column 
        |> Chart.WithOptions options
        |> Chart.WithLabels labels
        |> Chart.WithLegend true
        |> Chart.WithSize (800, 800)
        |> Chart.Show
    
