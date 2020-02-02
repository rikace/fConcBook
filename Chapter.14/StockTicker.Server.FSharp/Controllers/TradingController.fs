namespace StockTicker.Server.FSharp.Controllers

open System
open System.Reactive.Subjects
open StockTicker.Core
open StockTicker.Core.Logging
open StockTicker.Core.Logging.Message
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.SignalR
open StockTicker.Server.FSharp

//Listing 14.1 Web-API trading controller
type TradingController(hubContext: IHubContext<StockTickerHub.StockTicker>) =
    inherit Controller()

    static let logger = Log.create "StockTicker.TradingController"
    let logUserRequest methodName (tr : Order) =
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
        if subject.HasObservers then 
            match cmd with
            | Result.Ok(cmd) ->         // #B
                CommandWrapper.CreateTrading connectionId cmd// #C
                |> subject.OnNext       // #A
            | Result.Error(e) -> subject.OnError(exn (e))    // #B
        cmd
        
    let processRequest (connId: string) (order: ClientOrder) =
        Validation.tradingdValidation order
        |> log
        |> publish connId 
                
    [<HttpGet>]
    member this.Index() : IActionResult =
        base.View("Index") :> IActionResult
        
    [<HttpPost>]
    [<Route("trading/logportfolio")>]
    member this.LogPortfolio([<FromForm>]userMarket: User) =       
        let userName = userMarket.Username;
        let initialCash = userMarket.Initialcash;
        base.RedirectToAction("OpenDashboard",  {| userName = userName; initialCash = initialCash |} )
       
    [<HttpGet>]
    [<Route("trading/opendashboard")>]
    member this.OpenDashboard(userName: string, initialCash: decimal) =
        let userModel = 
            { User.Username = userName; Initialcash = initialCash; UserId = Guid.NewGuid().ToString("N") }

        // Load stocks by the username 
        base.View("Market", userModel)


    member private this.toResponse result =
        let response =
            match result with
            | Ok(_) -> base.StatusCode(200)
            | _ -> base.StatusCode(500)    // #D
        response            
    
    [<Route("trading/placeorder")>]
    [<HttpPost>]
    member this.PlaceOrder([<FromBody>]data: Order) =
        // can easily make asynchronous controller methods.
        Async.StartAsTask <| async {        // #H
        
        do! logUserRequest data.TradingType data
        let connectionId = data.ConnId
        
        let tradingType =
            if String.Compare("buy", data.TradingType, StringComparison.OrdinalIgnoreCase) = 0 then
                TradingType.Buy
            else TradingType.Sell;
            
        return {
            ClientOrder.Price = Decimal.Parse(data.Price)
            Quantity = data.Quantity
            Symbol = data.Symbol
            TradingType = tradingType
            UserId = data.UserId
        }
        // validation using function composition
        |> tradingdValidation           // #F
        |> log
        |> publish connectionId         // #G
        |> this.toResponse              // #D
    }
        
    // The controller behaves as Observable publisher and it can be register
    interface IObservable<CommandWrapper> with
        member this.Subscribe observer = subject.Subscribe observer  // #A

    override this.Dispose disposing =
        if disposing then subject.Dispose()
        base.Dispose disposing    // #I

