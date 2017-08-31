using System;
using System.Threading.Tasks;
using StockTicker.Core;

namespace StockTicker.Client
{
    // Listing 14.11 Client StockTicker interface to receive notification using SignalR
    public interface IStockTickerHub
	{
		Task Init(string serverUrl, IStockTickerHubClient client);
        string ConnectionId { get; }
        Task GetAllStocks();
        Task<string> GetMarketState();
        Task OpenMarket();
		Task CloseMarket();
	}
}
