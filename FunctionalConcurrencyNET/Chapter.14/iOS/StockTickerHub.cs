using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Xamarin.Forms;
using StockTicker.Core;
using StockTicker.Client.iOS;
using static StockTicker.Core.Models;

[assembly: Dependency(typeof(StockTickerHub))]
namespace StockTicker.Client.iOS
{
    public class StockTickerHub : IStockTickerHub
    {
        public StockTickerHub()
        { }

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
            proxy.On<string>("SetMarketState", (x) => Device.BeginInvokeOnMainThread(() => client.SetMarketState(x)));
            proxy.On<Stock>("UpdateStockPrice", (x) => Device.BeginInvokeOnMainThread(() => client.UpdateStockPrice(x)));
            proxy.On<Stock>("SetStock", (x) => Device.BeginInvokeOnMainThread(() => client.SetStock(x)));
            proxy.On<OrderRecord>("UpdateOrderBuy", (x) => Device.BeginInvokeOnMainThread(() => client.UpdateOrderBuy(x)));
            proxy.On<OrderRecord>("UpdateOrderSell", (x) => Device.BeginInvokeOnMainThread(() => client.UpdateOrderSell(x)));
            proxy.On<Asset>("UpdateAsset", (x) => Device.BeginInvokeOnMainThread(() => client.UpdateAsset(x)));
            proxy.On<double>("SetInitialAsset", (x) => Device.BeginInvokeOnMainThread(() => client.SetInitialAsset(x)));
        }

        public async Task GetAllStocks() => await stockTickerProxy.Invoke("GetAllStocks");

        public async Task<string> GetMarketState() => await stockTickerProxy.Invoke<string>("GetMarketState");

        public async Task OpenMarket() => await stockTickerProxy.Invoke("OpenMarket");

        public async Task CloseMarket() => await stockTickerProxy.Invoke("CloseMarket");
    }
}
