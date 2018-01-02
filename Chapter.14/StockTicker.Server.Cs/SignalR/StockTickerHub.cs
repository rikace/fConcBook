using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using StockTicker.Core;
using StockTicker.Server.Cs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using static StockTicker.Core.Models;

namespace StockTicker.Server.Cs.SignalR
{
    [HubName("stockTicker")]
    public class StockTickerHub : Hub<IStockTickerHubClient>
    {
        public StockTickerHub()
        {
            stockMarket = StockMarket.StockMarket.Instance();
            tradingCoordinator = TradingCoordinator.Instance();
        }

        private static int userCount = 0;
        private readonly StockMarket.StockMarket stockMarket;
        private readonly TradingCoordinator tradingCoordinator;

        public override Task OnConnected()
        {
            System.Threading.Interlocked.Increment(ref userCount);
            tradingCoordinator.Subscribe(Context.ConnectionId, 1000, Clients);
            return base.OnConnected();
        }
        public override Task OnDisconnected(bool stopCalled)
        {
            System.Threading.Interlocked.Decrement(ref userCount);
            tradingCoordinator.Unsubscribe(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public void GetAllStocks()
        {
            var stocks = stockMarket.GetAllStocks(Context.ConnectionId);
            foreach (var stock in stocks)
                Clients.Caller.SetStock(stock);
        }

        public void OpenMarket()
        {
            stockMarket.OpenMarket(Context.ConnectionId);
            Clients.All.SetMarketState(MarketState.Open.ToString());
        }

        public void CloseMarket()
        {
            stockMarket.CloseMarket(Context.ConnectionId);
            Clients.All.SetMarketState(MarketState.Closed.ToString());
        }

        public string GetMarketState()
            => stockMarket.GetMarketState(Context.ConnectionId).ToString();
    }
}