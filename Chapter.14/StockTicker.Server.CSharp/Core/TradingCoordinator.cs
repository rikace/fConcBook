using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ReactiveAgent.CSharp;
using StockTicker.Core;
using TwitterEmotionAnalysis.CSharp.RxPublisherSubscriber;

namespace StockTicker.Server.CSharp.Core
{
    using static TradingSupervisorAgent;

    public class TradingCoordinator : IDisposable
    {
        private static readonly Lazy<TradingCoordinator> tradingCoordinator =
            new Lazy<TradingCoordinator>(() => new TradingCoordinator());

        private readonly IAgent<CoordinatorMessage> coordinatorAgent;

        private readonly RxPubSub<Models.Trading> subject;

        private TradingCoordinator()
        {
            subject = new RxPubSub<Models.Trading>();
            coordinatorAgent =
                Agent.Start<Dictionary<string, (IObserver<Models.Trading>, IDisposable)>,
                    CoordinatorMessage>(
                    new Dictionary<string, (IObserver<Models.Trading>, IDisposable)>(),
                    async (agents, message) =>
                    {
                        switch (message)
                        {
                            case CoordinatorMessage.Subscribe msgSub:
                                var observer = new TradingAgent(msgSub.connId, msgSub.userName, msgSub.initialAmount,
                                    msgSub.caller);
                                var dispObsrever = subject.Subscribe(observer);
                                await msgSub.caller.SendAsync("setInitialAsset", msgSub.initialAmount);
                                agents.Add(msgSub.connId, (observer, dispObsrever));
                                break;
                            case CoordinatorMessage.Unsubscribe msgUnsub:
                                if (agents.TryGetValue(msgUnsub.id, out var value))
                                {
                                    value.Item2.Dispose();
                                    agents.Remove(msgUnsub.id);
                                }

                                break;
                            case CoordinatorMessage.PublishCommand msgCmd:
                                var id = msgCmd.connId;
                                if (agents.TryGetValue(id, out var agent))
                                    switch (msgCmd.Item2.Command)
                                    {
                                        case Models.TradingCommand.BuyStockCommand buy:
                                            agent.Item1.OnNext(Models.Trading.NewBuy(buy.tradingRecord.Symbol,
                                                buy.tradingRecord));

                                            break;
                                        case Models.TradingCommand.SellStockCommand sell:
                                            agent.Item1.OnNext(Models.Trading.NewSell(sell.tradingRecord.Symbol,
                                                sell.tradingRecord));
                                            break;
                                    }

                                break;
                        }

                        return agents;
                    });
        }

        public void Dispose()
        {
            subject.Dispose();
        }

        public Task Subscribe(string connId, string userName, decimal initialAmount, IClientProxy caller)
        {
            return coordinatorAgent.Send(CoordinatorMessage.NewSubscribe(connId, userName, initialAmount, caller));
        }

        public Task Unsubscribe(string connId)
        {
            return coordinatorAgent.Send(CoordinatorMessage.NewUnsubscribe(connId));
        }

        public Task PublishCommand(CoordinatorMessage command)
        {
            return coordinatorAgent.Send(command);
        }

        public IDisposable AddPublisher(IObservable<Models.Trading> observable)
        {
            return subject.AddPublisher(observable);
        }

        public static TradingCoordinator Instance()
        {
            return tradingCoordinator.Value;
        }
    }
}