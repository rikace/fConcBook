using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncInterop;
using Microsoft.AspNetCore.SignalR;
using StockTicker.Core;

namespace StockTicker.Server.CSharp.Core
{
    public class StockTicker : Hub, IStockTickerHubClient
    {
        private static int _userCount;
        private readonly StockMarket.StockMarket _stockMarket;
        private readonly TradingCoordinator _tradingCoordinator;

        public StockTicker()
        {
            _stockMarket = StockMarket.StockMarket.Instance();
            _tradingCoordinator = TradingCoordinator.Instance();
        }

        public Task Subscribe(string userName, decimal initialCash)
        {
            return _tradingCoordinator.Subscribe(Context.ConnectionId, userName, initialCash, Clients.Caller);
        }

        public Task GetAllStocks()
        {
            var stocks = _stockMarket.GetAllStocks(Context.ConnectionId);
            return Clients.Caller.SendAsync("setStocks", stocks.ToArray());
        }

        public void OpenMarket()
        {
            _stockMarket.OpenMarket(Context.ConnectionId);
        }

        public void CloseMarket()
        {
            _stockMarket.CloseMarket(Context.ConnectionId);
        }

        public async Task<string> GetMarketState()
        {
            var marketState = await _stockMarket.GetMarketState(Context.ConnectionId).AsTask();
            return marketState.ToString();
        }

        public override Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _userCount);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Interlocked.Decrement(ref _userCount);
            _tradingCoordinator.Unsubscribe(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}