namespace StockTicker.Server.FSharp

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open StockTicker.Core
open System.Reactive
open StockTicker.Server.FSharp.Controllers
open StockTicker.Server.FSharp.Bus.CommandHandler
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Mvc.Controllers
open Microsoft.AspNetCore.SignalR
open System.Text.Json
open System.Reactive.Subjects

[<RequireQualifiedAccess>]
module Observable =
    let subscribeObserver (obs: IObserver<'T>) (observable: IObservable<'T>) =
        observable.Subscribe(obs)
        
// Transform the web api controller in an Observable publisher,
// register the controller to the command dispatcher.
// Using Pub/Sub API of Reactive Extensions used to pipe messages to an Agent
// The Agent only depend on IObserver implementation reducing the dependencies
// Hook into the Web API framework where it creates controllers

// Listing 14.2 Register a Web-API controller as Observable
type ControlActivatorPublisher(requestObserver:IObserver<CommandWrapper>) =
    interface IControllerActivator with  // #A
        member this.Create(context: ControllerContext) =
            let controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType()
            if controllerType = typeof<TradingController> then   // #B
                let hubContext = context.HttpContext.RequestServices.GetRequiredService<IHubContext<StockTickerHub.StockTicker>>();
                let obsController =
                    let tradingCtrl = new TradingController(hubContext)                    
                    tradingCtrl
                    |> Observable.subscribeObserver requestObserver   // #B
                    |> ignore
                    tradingCtrl
                box obsController       // #B
                
            elif controllerType = typeof<PredictionController> then
                PredictionController() |> box
            else
                raise
                <| ArgumentException(
                    sprintf "Unknown controller type requested: %O" controllerType,
                    "controllerType")
                
        member this.Release(context:ControllerContext, controller:obj) = ()

(*  Startup Class
    The server needs to know which URL to intercept and direct to SignalR. To do that,
    we add an OWIN startup class.
 *)

// Route for ASP.NET Web API applications
//type HttpRoute = {
//    controller : string
//    id : RouteParameter }


type Startup private () =
    // Controller subscriber in for of agent
    // dispatch heterogeneous message depending on the incoming
    // Only message at the time
    let tradingCoordinatorCommanndAgent = new Agent<CommandWrapper>(fun inbox ->    // #A
            let rec loop () = async {
                let! (cmd:CommandWrapper) = inbox.Receive()
                do! cmd |> AsyncHandle    // #B
                return! loop() }
            loop())
    do tradingCoordinatorCommanndAgent.Start()    // #A
    
    let eventStoreCommandAgent = new Agent<CommandWrapper>(fun inbox ->    // #A
            let rec loop () = async {
                let! (cmd:CommandWrapper) = inbox.Receive()
                do! cmd |> AsyncHandle    // #B
                return! loop() }
            loop())
    do eventStoreCommandAgent.Start()    // #A    
        
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration
        

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        // replace the default controller activator
        services.AddSingleton<IControllerActivator>(fun provider ->   // #C
   
            let subject = new Subject<Models.CommandWrapper>()
   
            // This is a subscription controller to the Agent
            // Each time a message come in (post) the publisher send the message (OnNext)
            // to all the subscribers, in this case the Agent            
   
            subject.Subscribe(Observer.Create(fun x -> tradingCoordinatorCommanndAgent.Post(x))) |> ignore
            subject.Subscribe(Observer.Create(fun x -> eventStoreCommandAgent.Post(x))) |> ignore
   
   
            ControlActivatorPublisher(subject) :> IControllerActivator    // #D
        ) |> ignore

        services.AddControllers().AddJsonOptions(fun options ->
            options.JsonSerializerOptions.DictionaryKeyPolicy <- JsonNamingPolicy.CamelCase
            options.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        ) |> ignore
        services.AddRazorPages() |> ignore
        //  MapSignalR() is an extension method of IAppBuilder provided by SignalR to facilitate mapping and configuration of the hub service.
        //  The generic overload MapSignalR<TConnection> is used to map persistent connections, that we do not have to specify the classes that implement the services
        services.AddSignalR() |> ignore
        services.AddCors(fun options ->
            options.AddPolicy("CorsPolicy", Action<CorsPolicyBuilder>(fun builder ->
                builder.WithOrigins("http://localhost:5000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials() |> ignore))
        ) |> ignore

        services.AddMvc()
           .AddJsonOptions(fun options ->
               options.JsonSerializerOptions.DictionaryKeyPolicy <- JsonNamingPolicy.CamelCase
               options.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
           ).AddRazorPagesOptions(fun options ->
               options.Conventions.AddPageRoute("/Index", "") |> ignore) |> ignore

        services.AddControllersWithViews().AddRazorRuntimeCompilation() |> ignore
        services.AddRazorPages() |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =

        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseExceptionHandler("/Error") |> ignore
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts() |> ignore

        app.UseCors("CorsPolicy") |> ignore
        app.UseHttpsRedirection() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseRouting() |> ignore
        app.UseDefaultFiles() |> ignore
        app.UseFileServer() |> ignore

        app.UseEndpoints(fun endpoints ->
            endpoints.MapControllers()|> ignore
            endpoints.MapRazorPages() |> ignore
            endpoints.MapHub<StockTicker.Server.FSharp.StockTickerHub.StockTicker>("/stockticker") |> ignore
            endpoints.MapControllerRoute(
                name = "default",
                pattern = "{controller=Trading}/{action=Index}/{id?}") |> ignore
            ) |> ignore

        EventStorage.EventStorage.Instance() |> ignore
        let stockMarket = StockMarket.StockMarket.Instance()
        TradingCoordinator.TradingCoordinator.Instance().AddPublisher(stockMarket.AsObservable()) |> ignore
        
    member val Configuration : IConfiguration = null with get, set
