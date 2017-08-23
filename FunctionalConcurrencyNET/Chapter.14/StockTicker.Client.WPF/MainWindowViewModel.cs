using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using StockTicker.Client.Model;
using StockTicker.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.Client.WPF
{
    public class MainWindowViewModel : ModelObject, IStockTickerHubClient
    {
        public MainWindowViewModel(MainWindow page)
        {
            Stocks = new ObservableCollection<StockModelObject>();
            Portfolio = new ObservableCollection<Models.OrderRecord>();
            BuyOrders = new ObservableCollection<Models.OrderRecord>();
            SellOrders = new ObservableCollection<Models.OrderRecord>();

            SendBuyRequestCommand = new RelayCommand(async () => await SendBuyRequest());
            SendSellRequestCommand = new RelayCommand(async () => await SendSellRequest());
            PredictCommand = new RelayCommand(async () => await Predict());

            stockTickerHub = new StockTickerHub();
            hostPage = page;

            var hostBase = "http://localhost:8735/";
            stockTickerHub
                .Init(hostBase, this)
                .ContinueWith(async x =>
                {
                    // Request market state from the server.
                    var state = await stockTickerHub.GetMarketState();

                    // Update UI (we are on the UI thread)
                    isMarketOpen = state == "Open";
                    OnPropertyChanged(nameof(IsMarketOpen));
                    OnPropertyChanged(nameof(MarketStatusMessage));

                    // Request stock prices
                    await stockTickerHub.GetAllStocks();
                }, TaskScheduler.FromCurrentSynchronizationContext());

            // Initialize WebClient
            client = new HttpClient();
            client.BaseAddress = new Uri(hostBase);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // StockTicker SignalR Hub
        private IStockTickerHub stockTickerHub;

        private HttpClient client;
        private MainWindow hostPage;

        public ObservableCollection<StockModelObject> Stocks { get; }

        // Command for event bindings from UI
        public RelayCommand SendBuyRequestCommand { get; }
        public RelayCommand SendSellRequestCommand { get; }
        public RelayCommand PredictCommand { get; }

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
        public string Symbol
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
        private async Task SendTradingRequest(string url)
        {
            if (!Stocks.Any(x => String.Compare(x.Symbol, Symbol, true) ==0))
            {
                hostPage.DisplayAlert("Alert", $"Unknown stock symbol:'{Symbol}'", "OK");
                return;
            }
            if (Price < 0)
            {
                hostPage.DisplayAlert("Alert", $"Price '{Price}' cannot be negative", "OK");
                return;
            }
            if (Amount < 0)
            {
                hostPage.DisplayAlert("Alert", $"Amount '{Amount}' cannot be negative", "OK");
                return;
            }

            var request = new Models.TradingRequest(stockTickerHub.ConnectionId, Symbol, Price, Amount);
            var response = await client.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
        }

        private async Task SendBuyRequest()
        {
            await SendTradingRequest("/api/trading/buy");
        }

        private async Task SendSellRequest()
        {
            await SendTradingRequest("/api/trading/sell");
        }

        private async Task Predict()
        {
            Prediction = "Simulating ...";

            var stock = Stocks.FirstOrDefault(x => x.Symbol == Symbol);
            if (stock == null)
            {
                hostPage.DisplayAlert("Alert", $"Unknown stock symbol:'{Symbol}'", "OK");
                return;
            }
            if (Price < 0)
            {
                hostPage.DisplayAlert("Alert", $"Price '{Price}' cannot be negative", "OK");
                return;
            }
            if (NumTimesteps < 1)
            {
                hostPage.DisplayAlert("Alert", $"NumTimesteps '{NumTimesteps}' cannot be less than 1", "OK");
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
        public void SetMarketState(string value)
        {
            IsMarketOpen = (value == "Open");
        }

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
                cash = Math.Round(value, 2);
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

        public void UpdateOrderBuy(Models.OrderRecord value)
        {
            BuyOrders.Add(value);
        }

        public void UpdateOrderSell(Models.OrderRecord value)
        {
            SellOrders.Add(value);
        }

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

            foreach (var x in root)
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
