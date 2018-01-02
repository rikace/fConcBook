open Microsoft.Owin.Hosting
open Owin
open System
open System.Web.Http
open StockTicker
open StockTicker.Logging
open StockTicker.Logging.Message

let logger = Log.create "StockTicker.Server"
let logAccessPoint name url =
    logger.logWithAck Info (
       eventX "{logger}: {id} is running on {url}"
       >> setField "logger" (sprintf "%A" logger.name)
       >> setField "id" name
       >> setField "url" (Uri url))
    |> Async.StartImmediate


[<EntryPoint>]
let main argv =
    try
        let hostAddress = "http://localhost:8935"

        let startup  = Startup()
        use server = WebApp.Start(hostAddress, startup.Configuration)

        logAccessPoint "StockTicker.Server" hostAddress
        logAccessPoint "Swagger UI" (hostAddress+"/swagger/ui/index")
        logAccessPoint "Swagget JSON Schema" (hostAddress+"/swagger/docs/v1")

        printfn  "\nPress Enter key to stop"
        Console.ReadLine() |> ignore
    with
    | e ->
        logger.logWithAck Fatal (
           eventX "Application crashed"
           >> addExn e)
        |> Async.StartImmediate

        raise e
    0
