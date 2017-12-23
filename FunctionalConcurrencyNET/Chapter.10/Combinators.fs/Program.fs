module Program

open System

[<EntryPoint>]
let main argv =
    printfn "Enter stock symbol name: "
    let symbol = "AAPL"// Console.ReadLine()
    let x =
        StockAnalysis.doInvest symbol
        |> Async.RunSynchronously

    printfn "Recommendation for %s: %A" symbol x

    0
