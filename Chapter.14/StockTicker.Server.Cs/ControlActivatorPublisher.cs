using StockTicker.Server.Cs.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Dispatcher;
using System.Net.Http;
using System.Web.Http.Controllers;
using static StockTicker.Core.Models;

namespace StockTicker.Server.Cs
{
    public class ControlActivatorPublisher : IHttpControllerActivator
    {
        private IObserver<CommandWrapper> requestObserver;

        public ControlActivatorPublisher(IObserver<CommandWrapper> requestObserver) {
            this.requestObserver = requestObserver;
        }

        IHttpController IHttpControllerActivator.Create(
            HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType == typeof(TradingController))
            {
                var tradingCtrl = new TradingController();
                var sub = tradingCtrl.Subscribe(requestObserver);
                request.RegisterForDispose(sub);
                return tradingCtrl;
            }

            if (controllerType == typeof(PredictionController))
            {
                return new PredictionController();
            }

            throw new ArgumentException(
                    $"Unknown controller type requested: {controllerType}", nameof(controllerType));
        }
    }
}