using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Common;
using static Common.Serializer;
using static Common.ColorPrint;
using static Common.SecureStream;
namespace Server
{
    class Program
    {
        // Listing 13.8 Reactive ConnectServer
        static void ConnectServer(int port, string sslName = null)
        {
            var cts = new CancellationTokenSource();
            string[] stockFiles = new string[] { "aapl.csv", "amzn.csv", "fb.csv", "goog.csv", "msft.csv" }; // #A

            var formatter = new BinaryFormatter(); // #B

            TcpListener.Create(port)
                .ToAcceptTcpClientObservable() // #C
                .ObserveOn(TaskPoolScheduler.Default) // #D
                .Subscribe(client =>
                {
                    using (var stream = GetServerStream(client, sslName)) // #E
                    {
                        stockFiles
                            .ObservableStreams(StockData.Parse) // #F
                            .Subscribe(async stock =>
                            {
                                var data = Serialize(formatter, stock); // #G
                                await stream.WriteAsync(data, 0, data.Length, cts.Token); // #G
                                PrintStockInfo(stock);
                            });
                    }
                },
                    error => Console.WriteLine("Error: " + error.Message), // #H
                    () => Console.WriteLine("OnCompleted"), // #H
                    cts.Token);
        }

        static void Main(string[] args)
        {
            var port = 8080;
            ConnectServer(port);

            Console.WriteLine($"Server listening on '{port}' port...");
            Console.WriteLine("Press ENTER to stop the server");
            Console.ReadLine();
        }
    }
}



