
open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive
open GameOfLife
open ViewEngine
open Microsoft.AspNetCore.Mvc

let indexHandler () =
    htmlLayout Pages.indexView ()
    
let startGameOfLife =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            Game.run()
            return! json "OK" next ctx            
        }
             
let webApp =
    choose [
        GET >=>
            choose [
                routeCi "/" >=> indexHandler ()           
                routeCi "/start" >=> startGameOfLife                
            ]
        setStatusCode 404 >=> text "Not Found"
    ]
    
let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message
    
let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:5001")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore
           
let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseWebSockets()
        .UseMiddleware<WebSocketMiddleware.Middleware.WebSocketMiddleware>()
        .UseStaticFiles()
        .UseCookiePolicy()
        .UseGiraffe(webApp)
    
let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    
    services.Configure<CookiePolicyOptions>(fun (options : CookiePolicyOptions) ->
        options.CheckConsentNeeded <- fun _ -> true
        options.MinimumSameSitePolicy <- SameSiteMode.None
    ) |> ignore
    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1) |> ignore
    
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    
let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "Content")
    
    printfn "contentRoot - %s" contentRoot
    printfn "webRoot - %s" webRoot
    
    let webhost = 
       WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
    
    webhost.Run()           
    0
