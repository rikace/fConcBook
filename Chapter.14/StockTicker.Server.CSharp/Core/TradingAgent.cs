using System;
using Microsoft.AspNetCore.SignalR;
using ReactiveAgent.CSharp;
using StockTicker.Core;

namespace StockTicker.Server.CSharp.Core
{
    public class TradingAgent : IObserver<Models.Trading>
    {
        private readonly IClientProxy _caller;
        private readonly string _connId;
        private readonly string _userName;

        public TradingAgent(string connId, string userName, decimal initialAmount, IClientProxy caller)
        {
            _caller = caller;
            _userName = userName;
            _connId = connId;

            var initialState = Models.Asset.Default;
            initialState.Cash = initialAmount;

            GetAgent = Agent.Start<Models.Asset, Models.Trading>(initialState,
                async (asset, message) =>
                {
                    switch (message)
                    {
                        case Models.Trading.Kill _:
                            throw new NotImplementedException(
                                "Kill messages should not be send to Dataflow based Agents");
                        case Models.Trading.Error msgError:
                            throw msgError.Item;

                        case Models.Trading.Buy msgBuy:
                            var (buyOrders, orderBuy) =
                                HelperFunctions.setOrder(asset.BuyOrders, msgBuy.symbol, msgBuy.Item2);
                            asset.BuyOrders = buyOrders;
                            await _caller.SendAsync("updateOrderBuy", orderBuy);
                            break;

                        case Models.Trading.Sell msgSell:
                            var (sellOrders, orderSell) =
                                HelperFunctions.setOrder(asset.SellOrders, msgSell.symbol, msgSell.Item2);
                            asset.SellOrders = sellOrders;
                            await _caller.SendAsync("updateOrderSell", orderSell);
                            break;

                        case Models.Trading.UpdateStock msgStock:
                            await _caller.SendAsync("updateStockPrice", msgStock.Item);

                            var isPortfolioUpdated = false;

                            var (cashUpdated, portfolioUpdated, sellTreadsUpdated, buyTreadsUpdated) =
                                HelperFunctions.updatePortfolio(asset.Cash, msgStock.Item, asset.Portfolio,
                                    asset.SellOrders, asset.BuyOrders);

                            if (asset.Cash != cashUpdated || asset.Portfolio != portfolioUpdated)
                                isPortfolioUpdated = true;

                            asset.Cash = cashUpdated;
                            asset.Portfolio = portfolioUpdated;
                            asset.SellOrders = sellTreadsUpdated;
                            asset.BuyOrders = buyTreadsUpdated;

                            if (isPortfolioUpdated)
                                await _caller.SendAsync("updateAsset", asset);

                            break;
                    }

                    return asset;
                });
        }

        public IAgent<Models.Trading> GetAgent { get; }

        public void OnNext(Models.Trading value)
        {
            GetAgent.Post(value);
        }

        public void OnError(Exception error)
        {
            GetAgent.Post(Models.Trading.NewError(error));
        }

        public void OnCompleted()
        {
            if (GetAgent is IDisposable killer)
                killer.Dispose();
            else
                throw new Exception("Wrong agent is used (that cannot be killed)");
        }
    }
}