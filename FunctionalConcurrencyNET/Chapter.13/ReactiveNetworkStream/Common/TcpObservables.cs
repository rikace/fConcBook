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
public static IObservable<TcpClient> ToAcceptTcpClientObservable(this TcpListener listener, int backlog = 5)
{
    //start listening with a determinate clients buffer backlog
    listener.Start(backlog);

    return Observable.Create<TcpClient>(async (observer, token) =>
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                //accept newly clients from the listener
                var client = await listener.AcceptTcpClientAsync();
                //route the client to the observer
                //into an asynchronous task to let multiple clients connect altogether

                Task.Factory.StartNew(_ => observer.OnNext(client), token, TaskCreationOptions.LongRunning);
            }

        }
        catch (OperationCanceledException)
        {
            observer.OnCompleted();
        }
        catch (Exception error)
        {
            observer.OnError(error);
        }
        finally
        {
            listener.Stop();
        }
        return Disposable.Create(() =>
        {
            listener.Stop();
            listener.Server.Dispose();
        });
    });
}

public static IObservable<TcpClient> ToConnectClientObservable(this IPEndPoint endpoint)
{
    return Observable.Create<TcpClient>(async (observer, token) =>  {
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
            token.ThrowIfCancellationRequested();
            observer.OnNext(client);
            observer.OnCompleted();
        }
        catch (Exception error)
        {
            observer.OnError(error);
        }
        Disposable.Create(() => client.Dispose());
    });
}
    }
}