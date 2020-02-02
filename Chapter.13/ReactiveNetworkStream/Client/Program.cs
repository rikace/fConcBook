using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Common;
using static Common.Serializer;
using static Common.ColorPrint;
using static Common.SecureStream;

namespace Client
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<string, StockData> stockData =
            new ConcurrentDictionary<string, StockData>();

        [STAThread]
        private static void Main(string[] args)
        {
            ConnectClient("127.0.0.1", 8080);
            Console.ReadLine();
        }

        private static void ConnectClient(string ip, int port, string sslName = null)
        {
            var ctx = SynchronizationContext.Current;
            var sw = Stopwatch.StartNew();

            // Listing 13.11 The Client program
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var cts = new CancellationTokenSource();
            var formatter = new BinaryFormatter();

            endpoint.ToConnectClientObservable()
                .Subscribe(client =>
                    {
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
                                UpdateChart(stock, sw.ElapsedMilliseconds);
                                PrintStockInfo(stock);
                            });
                    },
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);
        }

        private static void UpdateChart(StockData d, long elapsedMilliseconds)
        {
            stockData.AddOrUpdate(d.Symbol, d, (k, v) => d != v ? d : v);
            Console.WriteLine(
                $"Symbol {d.Symbol} - Date {d.Date} Closing value {d.Close} - Difference {d.Open - d.Close}");
        }
    }
}