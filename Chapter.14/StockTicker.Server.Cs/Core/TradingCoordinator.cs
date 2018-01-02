using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StockTicker.Core;
using RxPublisherSubscriber;
using static StockTicker.Core.Models;
using ReactiveAgent.Agents;
using static TradingSupervisorAgent;
using Microsoft.AspNet.SignalR.Hubs;

namespace StockTicker.Server.Cs.Core
{
    public class TradingCoordinator : IDisposable
    {
        public TradingCoordinator()
        {
            subject = new RxPubSub<Trading>();
            coordinatorAgent =
                Agent.Start< Dictionary < string, (IObserver<Trading>, IDisposable) > , CoordinatorMessage >(
                    new Dictionary<string, (IObserver<Trading>, IDisposable)>(),
                    (agents, message) => {
                        if (message is CoordinatorMessage.Subscribe msgSub) {
                            var observer = new TradingAgent(msgSub.id, msgSub.initialAmount, msgSub.caller);
                            var dispObsrever = subject.Subscribe(observer);
                            msgSub.caller.Client(msgSub.id).SetInitialAsset(msgSub.initialAmount);
                            agents.Add(msgSub.id, (observer, dispObsrever));
                        }
                        else if (message is CoordinatorMessage.Unsubscribe msgUnsub)
                        {
                            if (agents.TryGetValue(msgUnsub.id, out var value))
                            {
                                value.Item2.Dispose();
                                agents.Remove(msgUnsub.id);
                            }
                        } else if (message is CoordinatorMessage.PublishCommand msgCmd)
                        {
                            var id = msgCmd.connId;
                            if (msgCmd.Item2.Command is TradingCommand.BuyStockCommand buy)
                            {
                                if (agents.TryGetValue(id, out var a))
                                {
                                    var tradingDetails = new TradingDetails(buy.tradingRecord.Quantity, buy.tradingRecord.Price, TradingType.Buy);
                                    a.Item1.OnNext(Trading.NewBuy(buy.tradingRecord.Symbol, tradingDetails));
                                }
                            }
                            else if (msgCmd.Item2.Command is TradingCommand.SellStockCommand sell)
                            {
                                if (agents.TryGetValue(id, out var a))
                                {
                                    var tradingDetails = new TradingDetails(sell.tradingRecord.Quantity, sell.tradingRecord.Price, TradingType.Sell);
                                    a.Item1.OnNext(Trading.NewSell(sell.tradingRecord.Symbol, tradingDetails));
                                }
                            }
                        }
                        return agents;
                    });
        }

        private readonly RxPubSub<Trading> subject;
        private readonly IAgent<CoordinatorMessage> coordinatorAgent;

        public void Subscribe(string id, float initialAmount, IHubCallerConnectionContext<IStockTickerHubClient> caller)
            => coordinatorAgent.Post(CoordinatorMessage.NewSubscribe(id, initialAmount, caller));

        public void Unsubscribe(string id)
            => coordinatorAgent.Post(CoordinatorMessage.NewUnsubscribe(id));

        public void PublishCommand(CoordinatorMessage command)
            => coordinatorAgent.Post(command);

        public IDisposable AddPublisher(IObservable<Trading> observable)
            => subject.AddPublisher(observable);

        private static Lazy<TradingCoordinator> tradingCoordinator =
            new Lazy<TradingCoordinator>(() => new TradingCoordinator());
        public static TradingCoordinator Instance() => tradingCoordinator.Value;

        public void Dispose()
        {
            subject.Dispose();
        }
    }
}