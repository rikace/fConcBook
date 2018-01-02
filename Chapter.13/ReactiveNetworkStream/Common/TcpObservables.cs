using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public static class TcpObservables
    {
        // Listing 13.9 Asynchronous and reactive ToAcceptTcpClientObservable Rx method
        public static IObservable<TcpClient> ToAcceptTcpClientObservable(this TcpListener listener, int backlog = 5)
        {
            //start listening with a determinate clients buffer backlog
            listener.Start(backlog); // #A

            return Observable.Create<TcpClient>(async (observer, token) => // #B
            {
                try
                {
                    while (!token.IsCancellationRequested) // #C
                    {
                        //accept newly clients from the listener
                        var client = await listener.AcceptTcpClientAsync(); // #D
                        //route the client to the observer
                        //into an asynchronous task to let multiple clients connect altogether

                        await Task.Factory.StartNew(_ => observer.OnNext(client), token, TaskCreationOptions.LongRunning); // #E
                    }
                    observer.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    observer.OnCompleted(); // #F
                }
                catch (Exception error)
                {
                    observer.OnError(error); // #F
                }
                finally
                {
                    listener.Stop();
                }
                return Disposable.Create(() => // #G
                {
                    listener.Stop();
                    listener.Server.Dispose();
                });
            });
        }

        // Listing 13.12 Custom Observable operator ToConnetClientObservable
        public static IObservable<TcpClient> ToConnectClientObservable(this IPEndPoint endpoint)
        {
            return Observable.Create<TcpClient>(async (observer, token) =>
            {
                var client = new TcpClient();
                try
                {
                    await client.ConnectAsync(endpoint.Address, endpoint.Port);
                    token.ThrowIfCancellationRequested();
                    observer.OnNext(client);
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
                return Disposable.Create(() => client.Dispose());
            });
        }
    }
}