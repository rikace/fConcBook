using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockTicker.Core;
using StockTicker.Server.CSharp.Bus;
using StockTicker.Server.CSharp.Core;

namespace StockTicker.Server.CSharp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var subject = new Subject<Models.CommandWrapper>();

                var tradingSupervisorAgent = new ActionBlock<Models.CommandWrapper>(async cmd =>
                    await CommandHandler.TradingCoordinatorHandle(cmd).ConfigureAwait(false));
                subject.Subscribe(tradingSupervisorAgent.AsObserver());

                var eventStorageAgent = new ActionBlock<Models.CommandWrapper>(cmd =>
                    CommandHandler.EventStorageHandle(cmd));
                subject.Subscribe(eventStorageAgent.AsObserver());

                return subject.AsObserver();
            });

            services.AddSingleton<IControllerActivator>(provider =>
            {
                var observer = provider.GetService<IObserver<Models.CommandWrapper>>();
                return new CustomControllerActivator(observer);
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
            services.AddRazorPages();
            services.AddSignalR();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder => builder.WithOrigins("http://localhost:5000")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                .AddRazorPagesOptions(options => { options.Conventions.AddPageRoute("/Index", ""); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days.
                // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("CorsPolicy");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseDefaultFiles();
            app.UseFileServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapHub<Core.StockTicker>("/stockticker");
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Trading}/{action=Index}/{id?}");
            });

            EventStorage.EventStorage.Instance();
            var stockMarket = StockMarket.StockMarket.Instance();

            TradingCoordinator.Instance()
                .AddPublisher(stockMarket.AsObservable());
        }
    }
}