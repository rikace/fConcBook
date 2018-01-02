using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Web.Http;
using Swashbuckle.Application;
using Microsoft.AspNet.SignalR;
using System.Web.Http.Owin;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Hubs;
using System.Web.Http.Dispatcher;
using System.Web.Http.Controllers;
using System.Reactive;
using StockTicker;
using System.Threading.Tasks.Dataflow;
using static StockTicker.Core.Models;
using StockTicker.Server.Cs.Core;
using StockTicker.Server.Cs.Bus;

[assembly: OwinStartup(typeof(StockTicker.Server.Cs.Startup))]
namespace StockTicker.Server.Cs
{
    public class Startup
    {
        public Startup()
        {
            agent = new ActionBlock<CommandWrapper>(cmd =>
                CommandHandler.Handle(cmd));
        }

        private readonly ActionBlock<CommandWrapper> agent;

        public void Configuration(IAppBuilder builder)
        {
            var config = new HttpConfiguration();
            // Configure routing
            config.MapHttpAttributeRoutes();

            // Configure serialization
            config.Formatters.XmlFormatter.UseXmlSerializer = true;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver
                            = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();

            config.Routes.MapHttpRoute("tradingApi", "api/trading/{id}",
                 new { controller = "Trading", id = RouteParameter.Optional });


            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}",
                 new { controller = "{controller}", id = RouteParameter.Optional });

            // replace the default controller activator
            config.Services.Replace(
                typeof(IHttpControllerActivator),
                new ControlActivatorPublisher(
                    Observer.Create<CommandWrapper>(
                        x => agent.Post<CommandWrapper>(x))));

            // Enable Swagger and Swagger UI
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "StockTicker API"))
                .EnableSwaggerUi();

            var configSignalR = new HubConfiguration() { EnableDetailedErrors = true };   // #E
            GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule());
            Owin.CorsExtensions.UseCors(builder, Microsoft.Owin.Cors.CorsOptions.AllowAll);

            TradingCoordinator.Instance()
                .AddPublisher(StockMarket.StockMarket.Instance().AsObservable());

            //  MapSignalR() is an extension method of IAppBuilder provided by SignalR to facilitate mapping and configuration of the hub service.
            //  The generic overload MapSignalR<TConnection> is used to map persistent connections, that we do not have to specify the classes that implement the services
            builder.MapSignalR(configSignalR);
            builder.UseWebApi(config);
        }
    }
}
