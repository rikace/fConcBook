open Combinators.FSharp
open System

[<EntryPoint>]
let main argv =
    printfn "Enter stock symbol name: "
    let symbol = "AAPL"// Console.ReadLine()
    let x =
        StockAnalysis.doInvest symbol
        |> Async.RunSynchronously

    printfn "Recommendation for %s: %A" symbol x

    Console.ReadLine() |> ignore

    0
