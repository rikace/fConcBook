using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Reactive.Concurrency;
using System.Runtime.Serialization;
using System.Reactive.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Common;
using static Common.Serializer;
using static Common.ColorPrint;
using static Common.SecureStream;

namespace Client
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var chart = new Chart {Dock = DockStyle.Fill};
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            var form = new Form {Visible = true, Width = 1000, Height = 500};
            form.Controls.Add(chart);

            ConnectClient("127.0.0.1", 8080, chart);

            Application.Run(form);
            Console.ReadLine();
        }

        private static void ConnectClient(string ip, int port, Chart chart, string sslName = null)
        {
            var ctx = SynchronizationContext.Current;
            var sw = Stopwatch.StartNew();

            // Listing 13.11 The Client program
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var cts = new CancellationTokenSource();
            var formatter = new BinaryFormatter();

            endpoint.ToConnectClientObservable()
                .Subscribe(client => {
                    GetClientStream(client, sslName)
                        .ReadObservable(0x1000, cts.Token)
                        .Select(rawData => Deserialize<StockData>(formatter, rawData))
                        // using groupBy, we are filtering this data into multiple observable. Each observable can have its own unique operations. Because of grouping, Throttling now acts independently on each stock symbol. only Stocks with identical StocksSymbols will be filtered within the given throttle time span.
                        // Throttling can be done based on the stream data itself (rather than just a timespan). For Example, in the following code, the DistinctUntilChanged operator filters on a specific field.
                        // in this case the DistinctUntilChanged ignores any data where the change in High price does not vary that 0.1%
                        .GroupBy(item => item.Symbol)
                        .SelectMany(group =>
                            group
                                .Throttle(TimeSpan.FromMilliseconds(20)) // #A
                                // this partition the stream by the Stock Symbol, stare a new thread for each partition
                                .ObserveOn(TaskPoolScheduler.Default)) // #B
                        .ObserveOn(ctx)
                        .Subscribe(stock =>
                        {
                            UpdateChart(chart, stock, sw.ElapsedMilliseconds);
                            PrintStockInfo(stock);
                        });
                    },
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);
        }

        static void UpdateChart(Chart chart, StockData d, long elapsedMilliseconds)
        {
            Series series = chart.Series.FirstOrDefault(x => x.LegendText == d.Symbol);
            if (series == null)
            {
                series = new Series
                {
                    LegendText = d.Symbol,
                    ChartType = SeriesChartType.Candlestick
                };
                chart.Series.Add(series);
                chart.Legends.Add(new Legend(d.Symbol));
            }

            series.Points.AddXY(d.Date, d.High, d.Low, d.Open, d.Close);

            chart.Titles.Clear();
            chart.Titles.Add($"Time elapsed {elapsedMilliseconds} ms");
        }
    }
}