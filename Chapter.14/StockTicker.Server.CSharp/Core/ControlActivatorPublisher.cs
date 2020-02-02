using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using StockTicker.Core;
using StockTicker.Server.CSharp.Controllers;

namespace StockTicker.Server.CSharp.Core
{
    public class CustomControllerActivator : IControllerActivator
    {
        private readonly IObserver<Models.CommandWrapper> requestObserver;

        public CustomControllerActivator(IObserver<Models.CommandWrapper> requestObserver)
        {
            this.requestObserver = requestObserver;
        }

        public object Create(ControllerContext context)
        {
            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();

            if (controllerType == typeof(TradingController))
            {
                var hubContext = context.HttpContext.RequestServices.GetRequiredService<IHubContext<StockTicker>>();

                var tradingCtrl = new TradingController(hubContext);
                var sub = tradingCtrl.Subscribe(requestObserver);
                return tradingCtrl;
            }

            if (controllerType == typeof(PredictionController)) return new PredictionController();

            return Activator.CreateInstance(controllerType);
        }

        public void Release(ControllerContext context, object controller)
        {
        }
    }
}