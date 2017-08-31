namespace StockTicker.Controllers

open System
open System.Net
open System.Net.Http
open System.Web.Http
open StockTicker
open StockTicker.Validation
open StockTicker.Commands
open System.Reactive.Subjects
open StockTicker.Core
open StockTicker.Logging
open StockTicker.Logging.Message

//Listing 14.1 Web-API trading controller
[<RoutePrefix("api/trading")>]
type TradingController() =
    inherit ApiController()

    static let logger = Log.create "StockTicker.TradingController"
    let logUserRequest methodName (tr : TradingRequest) =
        logger.logWithAck Info (
           eventX "{logger}: Called {url} with trading request {tr}"
           >> setField "logger" (sprintf "%A" logger.name)
           >> setField "url" ("/api/trading/"+methodName)
           >> setField "tr" ((sprintf "%A" tr).Replace("\n","")) )

    // the controller act as a observable publisher of messages
    // keep the controller loosely coupled using the Reactive Extensions
    // defining a Reactive Subject which will fire up
    // requests to the subscriber(s)
    let subject = new Subject<CommandWrapper>()  // #A

    let log res =
        logger.logWithAck Info (
            match res with
            | Result.Ok m ->
                eventX "{logger}: Validation successful - {req}"
                >> setField "req" ((sprintf "%A" m).Replace("\n",""))
            | Result.Error f ->
                eventX "{logger}: Validation failed - {msg}"
                >> setField "msg" f
            >> setField "logger" (sprintf "%A" logger.name))
        |> Async.RunSynchronously
        res

    let publish connectionId cmd =   // #G
        match cmd with
        | Result.Ok(cmd) ->         // #B
            CommandWrapper.CreateTrading connectionId cmd// #C
            |> subject.OnNext       // #A
        | Result.Error(e) -> subject.OnError(exn (e))    // #B
        cmd

    let toResponse (request : HttpRequestMessage) result =
        let response =
            match result with
            | Ok(_) -> request.CreateResponse(HttpStatusCode.OK)
            | _ -> request.CreateResponse(HttpStatusCode.BadRequest)    // #D
        response

    [<Route("sell"); HttpPost>]
    member this.PostSell([<FromBody>] tr : TradingRequest) =
        async {
            do! logUserRequest "sell" tr

            let connectionId = tr.ConnectionID  // #E
            return
                {   Symbol = tr.Symbol.ToUpper()
                    Quantity = tr.Quantity
                    Price = tr.Price
                    Trading = TradingType.Sell }
                // validation using function composition
                |> tradingdValidation           // #F
                |> log
                |> publish connectionId         // #G
                |> toResponse this.Request      // #D
        // can easily make asynchronous controller methods.
        } |> Async.StartAsTask                  // #H

    [<Route("buy"); HttpPost>]
    member this.PostBuy([<FromBody>] tr : TradingRequest) =
        async {
            do! logUserRequest "buy" tr
            let connectionId = tr.ConnectionID

            return
                { Symbol = (tr.Symbol.ToUpper())
                  Quantity = tr.Quantity
                  Price = tr.Price
                  Trading = TradingType.Buy }
                |> tradingdValidation
                |> log
                |> publish connectionId
                |> toResponse this.Request

        } |> Async.StartAsTask // can easily make asynchronous controller methods.

    // The controller behaves as Observable publisher and it can be register
    interface IObservable<CommandWrapper> with
        member this.Subscribe observer = subject.Subscribe observer  // #A

    override this.Dispose disposing =
        if disposing then subject.Dispose()
        base.Dispose disposing    // #I

