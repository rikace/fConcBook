using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using StockTicker.Core;
using static StockTicker.Core.Models;
using System.Windows;
using System.Windows.Threading;

namespace StockTicker.Client.WPF
{
    public class StockTickerHub : IStockTickerHub
	{
		public StockTickerHub()
		{
		}

        public async Task Init(string serverUrl, IStockTickerHubClient client)
		{
			// Connect to the server
			hubConnection = new HubConnection(serverUrl);

			// Create a proxy to the 'stockTicker' SignalR Hub
			stockTickerProxy = hubConnection.CreateHubProxy("stockTicker");
            // Register event handlers
            RegisterEventHandlers(stockTickerProxy, client);

			// Start the connection
			await hubConnection.Start();
		}

        private HubConnection hubConnection;
        private IHubProxy stockTickerProxy;

        public string ConnectionId
        {
            get
            {
                return hubConnection.ConnectionId;
            }
        }

        private void RegisterEventHandlers(IHubProxy proxy, IStockTickerHubClient client)
        {            
            proxy.On<string>("SetMarketState",      (x) => BeginInvokeOnMainThread(() => client.SetMarketState(x)));
            proxy.On<Stock>("UpdateStockPrice",     (x) => BeginInvokeOnMainThread(() => client.UpdateStockPrice(x)));
            proxy.On<Stock>("SetStock",             (x) => BeginInvokeOnMainThread(() => client.SetStock(x)));
            proxy.On<OrderRecord>("UpdateOrderBuy", (x) => BeginInvokeOnMainThread(() => client.UpdateOrderBuy(x)));
            proxy.On<OrderRecord>("UpdateOrderSell",(x) => BeginInvokeOnMainThread(() => client.UpdateOrderSell(x)));
            proxy.On<Asset>("UpdateAsset",          (x) => BeginInvokeOnMainThread(() => client.UpdateAsset(x)));
            proxy.On<double>("SetInitialAsset",     (x) => BeginInvokeOnMainThread(() => client.SetInitialAsset(x)));
        }

        private void BeginInvokeOnMainThread(Action p) => Application.Current?.Dispatcher?.Invoke(p);

        public async Task GetAllStocks() => await stockTickerProxy.Invoke("GetAllStocks");

        public async Task<string> GetMarketState() => await stockTickerProxy.Invoke<string>("GetMarketState");
        
        public async Task OpenMarket() => await stockTickerProxy.Invoke("OpenMarket");

		public async Task CloseMarket() => await stockTickerProxy.Invoke("CloseMarket");
	}
}
