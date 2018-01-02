namespace StockTicker

open Owin
open Microsoft.Owin
open System
open System.Net.Http
open System.Web.Http
open StockTicker.Controllers
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open System.Web.Http.Dispatcher
open System.Web.Http.Controllers
open System.Reactive
open FSharp.Control.Reactive
open StockTicker.Commands
open CommandHandler
open StockTicker.Core
open StockTicker.Server
open Swashbuckle.Application

// Transform the web api controller in an Observable publisher,
// register the controller to the command dispatcher.
// Using Pub/Sub API of Reactive Extensions used to pipe messages to an Agent
// The Agent only depend on IObserver implementation reducing the dependencies
// Hook into the Web API framework where it creates controllers

// Listing 14.2 Register a Web-API controller as Observable
type ControlActivatorPublisher(requestObserver:IObserver<CommandWrapper>) =
    interface IHttpControllerActivator with  // #A
        member this.Create(request, controllerDescriptor, controllerType) =
            if controllerType = typeof<TradingController> then   // #B
                let obsController =
                    let tradingCtrl = new TradingController()
                    tradingCtrl
                    |> Observable.subscribeObserver requestObserver   // #B
                    |> request.RegisterForDispose
                    tradingCtrl
                obsController :> IHttpController       // #B
            elif controllerType = typeof<PredictionController> then
                new PredictionController() :> IHttpController
            else
                raise
                <| ArgumentException(
                    sprintf "Unknown controller type requested: %O" controllerType,
                    "controllerType")

(*  Startup Class
    The server needs to know which URL to intercept and direct to SignalR. To do that,
    we add an OWIN startup class.
 *)

// Route for ASP.NET Web API applications
type HttpRoute = {
    controller : string
    id : RouteParameter }

type ErrorHandlingPipelineModule() =
    inherit HubPipelineModule()

    override x.OnIncomingError(exceptionContext:ExceptionContext, invokerContext:IHubIncomingInvokerContext) =
            System.Diagnostics.Debug.WriteLine("=> Exception " + exceptionContext.Error.Message)
            if exceptionContext.Error.InnerException <> null then
                System.Diagnostics.Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message)
            base.OnIncomingError(exceptionContext, invokerContext)


// Listing 14.3 Application startup to configure SignalR hub and agent message bus
[<Sealed>]
type Startup() =

    // Controller subscriber in for of agent
    // dispatch heterogeneous message depending on the incoming
    // Only message at the time
    let agent = new Agent<CommandWrapper>(fun inbox ->    // #A
            let rec loop () = async {
                let! (cmd:CommandWrapper) = inbox.Receive()
                do! cmd |> AsyncHandle    // #B
                return! loop() }
            loop())
    do agent.Start()    // #A

    // Additional Web API settings
    member this.Configuration(builder : IAppBuilder) =

        let config =
            let config = new HttpConfiguration()
            // Configure routing
            config.MapHttpAttributeRoutes()

            // Configure serialization
            config.Formatters.XmlFormatter.UseXmlSerializer <- true
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver
                            <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

            config.Routes.MapHttpRoute("tradingApi", "api/trading/{id}",
                 { controller = "Trading"; id = RouteParameter.Optional }) |> ignore


            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}",
                 { controller = "{controller}"; id = RouteParameter.Optional }) |> ignore

            // replace the default controller activator
            config.Services.Replace(typeof<IHttpControllerActivator>,    // #C
                                    // This is a subscription controller to the Agent
                                    // Each time a message come in (post) the publisher send the message (OnNext)
                                    // to all the subscribers, in this case the Agent
                                    ControlActivatorPublisher( Observer.Create(fun x -> agent.Post(x)) ))  // #D

            // Enable Swagger and Swagger UI
            config
                .EnableSwagger(fun c -> c.SingleApiVersion("v1", "StockTicker API") |> ignore)
                .EnableSwaggerUi();

            config

        let configSignalR = new HubConfiguration(EnableDetailedErrors = true)   // #E
        GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule()) |> ignore
        Owin.CorsExtensions.UseCors(builder, Cors.CorsOptions.AllowAll) |> ignore

        TradingCoordinator.Instance().AddPublisher(StockMarket.StockMarket.Instance().AsObservable()) |> ignore

        //  MapSignalR() is an extension method of IAppBuilder provided by SignalR to facilitate mapping and configuration of the hub service.
        //  The generic overload MapSignalR<TConnection> is used to map persistent connections, that we do not have to specify the classes that implement the services
        builder.MapSignalR(configSignalR) |> ignore
        builder.UseWebApi(config) |> ignore
