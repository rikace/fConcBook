using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using RxPipeServer;
using static Common.Serialzer;
using static Common.ColorPrint;
using static Common.SecureStream;
namespace Server
{
    class Program
    {
        static void ConnectServer(int port, string sslName = null)
        {
            var cts = new CancellationTokenSource();
            string[] stockFiles = new string[] { "aapl.csv", "amzn.csv", "fb.csv", "goog.csv", "msft.csv" };

            var formatter = new BinaryFormatter();

            //convert a TcpListener into an observable sequence on port 23 (telnet)
            TcpListener.Create(port)
                .ToAcceptTcpClientObservable()
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(client =>
                {
                    using (var stream = GetServerStream(client, sslName))
                    {
                        stockFiles
                            .ObservableStreams(StockData.Parse)
                            .Subscribe(async stock =>
                            {
                                var data = Serialize(formatter, stock);
                                await stream.WriteAsync(data, 0, data.Length, cts.Token);
                                PrintStockInfo(stock);
                            });
                    }
                },
                    error => Console.WriteLine("Error: " + error.Message),
                    () => Console.WriteLine("OnCompleted"),
                    cts.Token);



            //while (!networkStream.EndOfStream) //still has some data
            //{

            ////convert a TcpListener into an observable sequence on port 23 (telnet)
            ////var tcpClientsSequence = TcpListener.Create(23)
            ////    .AcceptObservableClient()
            ////    .AsNetworkByteSource();


            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            //string[] stockFiles = new string[] { "aapl.csv", "amzn.csv", "fb.csv", "goog.csv", "msft.csv" };
            //var formatter = new BinaryFormatter();

            //var server = new PipeServerAsync("myPipe");
            //var disposable = server.Connect()
            //    .ToObservable()
            //    .Subscribe(_ =>
            //    {
            //        Console.WriteLine("Client Connected...");

            //        stockFiles
            //        .ObservableStreams(StockData.Parse)
            //        .Subscribe(stock =>
            //            {
            //                var data = Serialize(formatter, stock);
            //                server.Write(data);
            //                PrintStockInfo(stock);
            //            });

            //    });

            ConnectServer(8080);


            Console.WriteLine("Server listening...");
            Console.WriteLine("Press ENTER to stop the server");
            Console.ReadLine();
            // disposable.Dispose();
        }
    }
}



