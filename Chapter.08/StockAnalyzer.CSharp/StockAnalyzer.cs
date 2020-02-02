using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Functional.CSharp.Concurrency.Async;
using Microsoft.FSharp.Core;
using XPlot.GoogleCharts;

namespace StockAnalyzer.CSharp
{
    public struct StockData
    {
        public StockData(DateTime date, double open, double high, double low, double close)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public DateTime Date { get; }
        public double Open { get; }
        public double High { get; }
        public double Low { get; }
        public double Close { get; }
    }

    public class StockAnalyzer
    {
        public static readonly string[] Stocks =
            {"MSFT", "FB", "AAPL", "YHOO", "EBAY", "INTC", "GOOG", "ORCL"};

        //Listing 8.13 The Or combinator applies to falls back behavior
        private readonly Func<string, string> alphavantageSourceUrl = symbol => // #A
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&outputsize=full&apikey=W3LUV5WID6C0PV5L&datatype=csv";

        // Listing 8.6 Cancellation of Asynchronous Task
        private CancellationTokenSource cts = new CancellationTokenSource(); // #A

        private readonly Func<string, string> stooqSourceUrl = symbol => // #A
            $"https://stooq.com/q/d/l/?s={symbol}.US&i=d";

        private string CreateFinanceUrl(string symbol)
        {
            return
                $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&outputsize=full&apikey=W3LUV5WID6C0PV5L&datatype=csv";
        }

        // Listing 8.4 Stock prices history analysis
        private async Task<StockData[]> ConvertStockHistory(string stockHistory) // #A
        {
            return await Task.Run(() =>
            {
                // #B
                var stockHistoryRows =
                    stockHistory.Split(Environment.NewLine.ToCharArray(),
                        StringSplitOptions.RemoveEmptyEntries);
                return (from row in stockHistoryRows.Skip(1)
                        let cells = row.Split(',')
                        let date = DateTime.Parse(cells[0])
                        let open = double.Parse(cells[1] == "-" ? cells[3] : cells[1])
                        let high = double.Parse(cells[2] == "-" ? cells[4] : cells[2])
                        let low = double.Parse(cells[3])
                        let close = double.Parse(cells[4])
                        select new StockData(date, open, high, low, close)
                    ).ToArray();
            });
        } // #A

        private async Task<string> DownloadStockHistory(string symbol)
        {
            var stockUrl = CreateFinanceUrl(symbol);
            var request = WebRequest.Create(stockUrl); // #C
            using (var response = await request.GetResponseAsync()
                .ConfigureAwait(false)) // #D
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false); // #E
            }
        }

        private async Task<Tuple<string, StockData[]>> ProcessStockHistory(string symbol)
        {
            var stockHistory = await DownloadStockHistory(symbol); // #F
            var stockData = await ConvertStockHistory(stockHistory); // #F
            return Tuple.Create(symbol, stockData); // #G
        }

        public async Task AnalyzeStockHistory(string[] stockSymbols)
        {
            var sw = Stopwatch.StartNew();

            var stockHistoryTasks =
                stockSymbols.Select(stock => ProcessStockHistory(stock)); // #H

            var stockHistories = new List<Tuple<string, StockData[]>>();
            foreach (var stockTask in stockHistoryTasks)
                stockHistories.Add(await stockTask); // #I

            ShowChart(stockHistories, sw.ElapsedMilliseconds); // #L
        }

        private async Task<string> DownloadStockHistory(string symbol,
            CancellationToken token) // #B
        {
            var stockUrl = CreateFinanceUrl(symbol);
            var request = await new HttpClient().GetAsync(stockUrl, token); // #B
            return await request.Content.ReadAsStringAsync();
        }
        //cts.Cancel();  // #C

        private async Task AnalyzeStockHistory(string[] stockSymbols,
            CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            //Listing 8.7 Cancellation of Asynchronous operation manual checks
            var stockHistoryTasks =
                stockSymbols.Select(async symbol =>
                {
                    var stockUrl = CreateFinanceUrl(symbol);
                    var request = WebRequest.Create(stockUrl);
                    using (var response = await request.GetResponseAsync())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        token.ThrowIfCancellationRequested();

                        var csvData = await reader.ReadToEndAsync();
                        var prices = await ConvertStockHistory(csvData);

                        token.ThrowIfCancellationRequested();
                        return Tuple.Create(symbol, prices.ToArray());
                    }
                }).ToList();

            await Task.WhenAll(stockHistoryTasks)
                .ContinueWith(stockData => ShowChart(stockData.Result, sw.ElapsedMilliseconds), token); // #L
        }

        //Listing 8.10 The Bind operator in action
        private async Task<Tuple<string, StockData[]>> ProcessStockHistoryBind(string symbol)
        {
            return await DownloadStockHistory(symbol)
                .Bind(stockHistory => ConvertStockHistory(stockHistory)) //#A
                .Bind(stockData => Task.FromResult(Tuple.Create(symbol,
                    stockData))); //#A
        }

        private async Task<Tuple<string, StockData[]>> ProcessStockHistoryRetry(string symbol)
        {
            var stockHistory =
                await AsyncEx.Retry(() => DownloadStockHistory(symbol), 5, TimeSpan.FromSeconds(2));
            var stockData = await ConvertStockHistory(stockHistory);
            return Tuple.Create(symbol, stockData);
        }

        private async Task<string> DownloadStockHistory(Func<string, string> sourceStock,
            string symbol)
        {
            var stockUrl = sourceStock(symbol); // #B
            var request = WebRequest.Create(stockUrl);
            using (var response = await request.GetResponseAsync())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task<Tuple<string, StockData[]>> ProcessStockHistoryConditional(string symbol)
        {
            Func<Func<string, string>, Func<string, Task<string>>> downloadStock =
                service => stock => DownloadStockHistory(service, stock); // #C

            var alphavantageService =
                downloadStock(alphavantageSourceUrl); // #D
            var stoodService =
                downloadStock(stooqSourceUrl); // #D

            return await AsyncEx.Retry( // #E
                    () => alphavantageService(symbol)
                        .Otherwise(() => stoodService(symbol)), //#F
                    5, TimeSpan.FromSeconds(2))
                .Bind(data => ConvertStockHistory(data)) // #G
                .Map(prices => Tuple.Create(symbol, prices)); // #H
        }

        //Listing 8.14 Running Stock-History analysis  in parallel
        private async Task ProcessStockHistoryParallel()
        {
            var sw = Stopwatch.StartNew();
            var stockHistoryTasks =
                Stocks.Select(ProcessStockHistory).ToList(); // #A

            var stockHistories =
                await Task.WhenAll(stockHistoryTasks); // #B

            ShowChart(stockHistories, sw.ElapsedMilliseconds);
        }

        //Listing 8.15 Stock-History analysis processing as each Task completes
        public async Task ProcessStockHistoryAsComplete()
        {
            var sw = Stopwatch.StartNew();

            var stockHistoryTasks =
                Stocks.Select(ProcessStockHistoryConditional).ToList();

            var data = new List<Tuple<string, Data.Series>>();
            while (stockHistoryTasks.Count > 0) // #A
            {
                var stockHistoryTask =
                    await Task.WhenAny(stockHistoryTasks); // #B

                stockHistoryTasks.Remove(stockHistoryTask); // #C
                var stockHistory = await stockHistoryTask;

                var datums =
                    from stock in stockHistory.Item2
                    select new Data.Datum(stock.Date, stock.Open, stock.High, stock.Low, stock.Close);

                data.Add(Tuple.Create(stockHistory.Item1, new Data.Series(datums)));
            }

            UpdateChart(data, sw.ElapsedMilliseconds); // #D

            // Delay induced in purpose 
            Thread.Sleep(500);
        }

        private void ShowChart(IEnumerable<Tuple<string, StockData[]>> stockHistories, long elapsedTime)
        {
            var enumerable = stockHistories as Tuple<string, StockData[]>[] ?? stockHistories.ToArray();
            var series =
                from stock in enumerable
                let datums = stock.Item2.Select(r => new Data.Datum(r.Date, r.Open, r.High, r.Low, r.Close))
                select new Data.Series(datums);

            var googleChart = GoogleChart.Create(series,
                FSharpOption<IEnumerable<string>>.Some(enumerable.Select(s => s.Item1)), new Configuration.Options(),
                ChartGallery.Candlestick);

            var chart = Chart.WithTitle("MainArea", googleChart);
            chart.WithTitle($"Time elapsed {elapsedTime} ms");

            chart.Show();
        }

        private void UpdateChart(List<Tuple<string, Data.Series>> data, long elapsedTime)
        {
            var series = data.Select(r => r.Item2);
            var labels = data.Select(r => r.Item1);

            var googleChart = GoogleChart.Create(series, FSharpOption<IEnumerable<string>>.Some(labels),
                new Configuration.Options(),
                ChartGallery.Candlestick);

            var chart = Chart.WithTitle("MainArea", googleChart);
            chart.WithTitle($"Time elapsed {elapsedTime} ms");

            chart.Show();
        }
    }
}