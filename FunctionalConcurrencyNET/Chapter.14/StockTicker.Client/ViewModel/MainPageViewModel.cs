using StockTicker.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using StockTicker.Client.Model;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using static StockTicker.Core.Models;

namespace StockTicker.Client
{
    // Listing 14.12 Client side – mobile application using Xamarin.Forms
    public class MainPageViewModel : ModelObject, IStockTickerHubClient
    {
        public MainPageViewModel(Page page)
        {
            Stocks = new ObservableCollection<StockModelObject>();
            Portfolio = new ObservableCollection<Models.OrderRecord>();
            BuyOrders = new ObservableCollection<Models.OrderRecord>();
            SellOrders = new ObservableCollection<Models.OrderRecord>(); // #A

            SendBuyRequestCommand = new Command(async () => await SendBuyRequest());
            SendSellRequestCommand = new Command(async () => await SendSellRequest()); // #B
            PredictCommand = new Command(async () => await Predict());

            stockTickerHub = DependencyService.Get<IStockTickerHub>(); // #C
            hostPage = page;

            var hostBase = "http://localhost:8735/";
            stockTickerHub                  // #C
                .Init(hostBase, this)
                .ContinueWith(async x =>
                {
                    // Request market state from the server
                    var state = await stockTickerHub.GetMarketState();

                    // Update UI (we are on the UI thread)
                    isMarketOpen = state == "Open";
                    OnPropertyChanged(nameof(IsMarketOpen));
                    OnPropertyChanged(nameof(MarketStatusMessage));

                    // Request stock prices
                    await stockTickerHub.GetAllStocks();
                }, TaskScheduler.FromCurrentSynchronizationContext()); // #D

            // Initialize WebClient
            client = new HttpClient();
            client.BaseAddress = new Uri(hostBase);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")); // #E
        }

        // StockTicker SignalR Hub
        private IStockTickerHub stockTickerHub;

        private HttpClient client;
        private Page hostPage;

        public ObservableCollection<StockModelObject> Stocks { get; } // #A

        // Command for event bindings from UI
        public Command SendBuyRequestCommand { get; }
        public Command SendSellRequestCommand { get; } // #B
        public Command PredictCommand { get; }

        private bool isMarketOpen;
        public bool IsMarketOpen
        {
            get => isMarketOpen; set
            {
                if (value == isMarketOpen)
                    return;

                if (value)
                    stockTickerHub.OpenMarket();
                else
                    stockTickerHub.CloseMarket();

                isMarketOpen = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(MarketStatusMessage));
            }
        }

        public string MarketStatusMessage
        {
            get
            {
                var status = (IsMarketOpen) ? "Open" : "Closed";
                return $"StockTicker Market is {status}.";
            }
        }

        private string symbol;
        public string Symbol // #F
        {
            get => symbol; set
            {
                if (symbol == value)
                    return;
                symbol = value;
                OnPropertyChanged();
            }
        }

        private double price;
        public double Price
        {
            get => price; set
            {
                if (price == value)
                    return;
                price = value;
                OnPropertyChanged();
            }
        }

        private int amount;
        public int Amount
        {
            get => amount; set
            {
                if (amount == value)
                    return;
                amount = value;
                OnPropertyChanged();
            }
        }

        private int numTimesteps = 100;
        public int NumTimesteps
        {
            get => numTimesteps; set
            {
                if (numTimesteps == value)
                    return;
                numTimesteps = value;
                OnPropertyChanged();
            }
        }

        private string prediction;
        public string Prediction
        {
            get => prediction; set
            {
                if (prediction == value)
                    return;
                prediction = value;
                OnPropertyChanged();
            }
        }

        private async Task SendTradingRequest(string url) // #G
        {
            if(await Validate())
            {
                var request = new TradingRequest(stockTickerHub.ConnectionId, Symbol, Price, Amount);
                var response = await client.PostAsJsonAsync(url, request);
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task<bool> Validate()
        {
            if (!Stocks.Any(x => x.Symbol == Symbol))
            {
                await hostPage.DisplayAlert("Alert", $"Unknown stock symbol:'{Symbol}'", "OK");
                return false;
            }
            else if (Price < 0)
            {
                await hostPage.DisplayAlert("Alert", $"Price '{Price}' cannot be negative", "OK");
                return false;
            }
            else if (Amount < 0)
            {
                await hostPage.DisplayAlert("Alert", $"Amount '{Amount}' cannot be negative", "OK");
                return false;
            }
            else return true;
        }

        private async Task SendBuyRequest() =>  await SendTradingRequest("/api/trading/buy"); // #G

        private async Task SendSellRequest() => await SendTradingRequest("/api/trading/sell"); // #G

        private async Task Predict()
        {
            Prediction = "Simulating ...";

            var stock = Stocks.FirstOrDefault(x => x.Symbol == Symbol);
            if (stock == null)
            {
                await hostPage.DisplayAlert("Alert", $"Unknown stock symbol:'{Symbol}'", "OK");
                return;
            }
			if (Price < 0)
			{
				await hostPage.DisplayAlert("Alert", $"Price '{Price}' cannot be negative", "OK");
				return;
			}
            if (NumTimesteps < 1)
            {
                await hostPage.DisplayAlert("Alert", $"NumTimesteps '{NumTimesteps}' cannot be less than 1", "OK");
                return;
            }

            var request = new Models.PredictionRequest(stock.Symbol, Price, NumTimesteps);
            var response = await client.PostAsJsonAsync("/api/prediction/predict", request);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<Models.PredictionResponse>(jsonString);

            Prediction = $"MeanPrice = {resp.MeanPrice.ToString("N2")}";
        }

        // IStockTickerHubClient
        public void SetMarketState(string value) => IsMarketOpen = (value == "Open");

        public void UpdateStockPrice(Models.Stock value)
        {
            if (value.Index >= Stocks.Count ||
                value.Index != Stocks[value.Index].Index)
                return;// Probably market is open, but we did not receive all stocks yet

            Stocks[value.Index].Update(value);
        }

        private double cash;
        public double Cash
        {
            get => cash; set
            {
                if (cash == value)
                    return;
                cash = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Models.OrderRecord> Portfolio { get; }
        public ObservableCollection<Models.OrderRecord> BuyOrders { get; }
        public ObservableCollection<Models.OrderRecord> SellOrders { get; }

        public void SetStock(Models.Stock value)
        {
            if (Stocks.Count == value.Index)
                Stocks.Add(new StockModelObject(value));
        }

        public void UpdateOrderBuy(Models.OrderRecord value) => BuyOrders.Add(value); // #H

        public void UpdateOrderSell(Models.OrderRecord value) => SellOrders.Add(value); // #H

        public void UpdateAsset(Models.Asset value)
        {
            Cash = value.Cash;

            SyncCollections(Portfolio, value.Portfolio);
            SyncCollections(BuyOrders, value.BuyOrders);
            SyncCollections(SellOrders, value.SellOrders);
        }

        private void SyncCollections(ObservableCollection<Models.OrderRecord> root, List<Models.OrderRecord> update)
        {
            var addCandidates = new HashSet<Models.OrderRecord>(update);
            var removeCandidates = new List<Models.OrderRecord>();

            foreach(var x in root)
            {
                if (!addCandidates.Remove(x))
                    removeCandidates.Add(x);
            }

            foreach (var x in removeCandidates)
                root.Remove(x);
            foreach (var x in addCandidates)
                root.Add(x);
        }

        public void SetInitialAsset(double value)
        {
            Cash = value;
        }
    }
}
