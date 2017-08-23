using System;
using System.Threading.Tasks;
using StockTicker.Core;

namespace StockTicker.Client
{
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
