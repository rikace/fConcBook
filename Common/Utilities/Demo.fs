module Demo

open System
open PerfUtil

[<CompiledNameAttribute("PrintSeparator")>]
let printSeparator () =
    printfn "--------------------------------------------\n"

let benchmark name func =
    printfn "%s" name
    let perfResult = Benchmark.Run func
    printfn "PerfResult:\n\tElapsed=%A;\n\tCpuTime=%A;\n\tGcDelta=%A\n"
        perfResult.Elapsed perfResult.CpuTime perfResult.GcDelta

let Benchmark name (func:Action) =
    (fun () -> func.Invoke())
    |> benchmark name