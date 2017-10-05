using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public static class ObservableDataStreams
    {
public static IObservable<StockData> ObservableStreams
    (this IEnumerable<string> filePaths, Func<string, string, StockData> map, int delay = 50)
{
    return filePaths
        .Select(key => new FileLinesStream<StockData>(key, row => map(key, row)))
        .Select(fsStock =>
            {
                var startData = new DateTime(2001, 1, 1);
                return Observable
                        .Interval(TimeSpan.FromMilliseconds(delay))
                        .Zip(fsStock.ObserveLines(), (tick, stock) => {
                            stock.Date = startData + TimeSpan.FromDays(tick);
                            return stock;
                        });
            }
        )
        .Aggregate((o1, o2) => o1.Merge(o2));
}

public static IObservable<ArraySegment<byte>> ReadObservable(this Stream stream, int bufferSize, CancellationToken token = default(CancellationToken))
{
    var buffer = new byte[bufferSize];
    var asyncRead = Observable.FromAsync<int>(ct => stream.ReadAsync(buffer, 0, bufferSize, ct));
    return Observable.While(
            // while there is data to be read
            () => !token.IsCancellationRequested && stream.CanRead,
            // iteratively invoke the observable factory, which will
            // "recreate" it such that it will start from the current
            // stream position - hence "0" for offset
            // Defer because cold vs hot Observable
            Observable.Defer(() =>
                    !token.IsCancellationRequested && stream.CanRead
                        ? asyncRead
                        : Observable.Empty<int>())

                // When BeginRead() or EndRead() causes an exception
                .Catch((Func<Exception, IObservable<int>>)(ex => Observable.Empty<int>()))
                .TakeWhile(returnBuffer => returnBuffer > 0)
                .Select(readBytes => new ArraySegment<byte>(buffer, 0, readBytes)))
        //   .Select(readBytes => buffer.Take(readBytes).ToArray()))
        .Finally(stream.Dispose);
}
    }
}

//observer.OnNext(new ArraySegment<byte>(buffer, 0, received));
