using Microsoft.AspNet.SignalR.Hubs;
using ReactiveAgent.Agents;
using ReactiveAgent.Agents.Dataflow;
using StockTicker.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static StockTicker.Core.Models;

namespace StockTicker.Server.Cs.Core
{
    public class TradingAgent : IObserver<Trading>
    {
        class State
        {
            public double Cash { get; set; }
            public Dictionary<string, TradingDetails> Portfolio { get; set; }
            public Dictionary<string, List<TradingDetails>> BuyOrders { get; set; }
            public Dictionary<string, List<TradingDetails>> SellOrders { get; set; }
        }

        public TradingAgent (string connId, double initialAmount, IHubCallerConnectionContext<IStockTickerHubClient> caller)
        {
            var initialState = new State
            {
                Cash = initialAmount,
                Portfolio = new Dictionary<string, TradingDetails>(),
                BuyOrders = new Dictionary<string, List<TradingDetails>>(),
                SellOrders = new Dictionary<string, List<TradingDetails>>()
            };

            agent = Agent.Start<State, Trading>(initialState,
                (state, message)=>
                {
                    if (message is Trading.Kill)
                    {
                        throw new NotImplementedException("Kill messages should not be send to Dataflow based Agents");
                    }
                    else if (message is Trading.Error msgError)
                    {
                        throw msgError.Item;
                    }
                    else if (message is Trading.Buy msgBuy)
                    {
                        var items = HelperFunctions.setOrder(state.BuyOrders, msgBuy.symbol, msgBuy.Item2);
                        var order = HelperFunctions.createOrder(msgBuy.symbol, msgBuy.Item2, TradingType.Buy);
                        caller.Client(connId).UpdateOrderBuy(order);
                        state.BuyOrders = items;
                    }
                    else if (message is Trading.Sell msgSell)
                    {
                        var items = HelperFunctions.setOrder(state.SellOrders, msgSell.symbol, msgSell.Item2);
                        var order = HelperFunctions.createOrder(msgSell.symbol, msgSell.Item2, TradingType.Sell);
                        caller.Client(connId).UpdateOrderSell(order);
                        state.SellOrders = items;
                    }
                    else if (message is Trading.UpdateStock msgStock)
                    {
                        caller.Client(connId).UpdateStockPrice(msgStock.Item);

                        var x = HelperFunctions.updatePortfolio(state.Cash, msgStock.Item, state.Portfolio, state.SellOrders, TradingType.Sell);
                        state.Cash = x.Item1;
                        state.Portfolio = x.Item2;
                        state.SellOrders = x.Item3;

                        var y = HelperFunctions.updatePortfolio(state.Cash, msgStock.Item, state.Portfolio, state.BuyOrders, TradingType.Buy);
                        state.Cash = y.Item1;
                        state.Portfolio = y.Item2;
                        state.BuyOrders = y.Item3;

                        var asset = HelperFunctions.getUpdatedAsset(state.Portfolio, state.SellOrders, state.BuyOrders, state.Cash);
                        caller.Client(connId).UpdateAsset(asset);
                    }
                    return state;
                });
        }

        private readonly IAgent<Trading> agent;

        public IAgent<Trading> GetAgent => agent;

        public void OnNext(Trading value)
            => agent.Post(value);

        public void OnError(Exception error)
            => agent.Post(Trading.NewError(error));

        public void OnCompleted()
        {
            if (agent is IDisposable killer)
                killer.Dispose();
            else
                throw new Exception("Wrong agent is used (that cannot be killed)");
        }
    }
}