using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowObjectPoolEncryption
{
    // Provides a thread-safe object pool
    public class ObjectPoolAsync<T> : IDisposable
    {
        private readonly BufferBlock<T> buffer;
        private readonly Func<T> factory;
        private readonly CancellationToken ctsToken;
        private readonly int msecTimeout;
        private int currentSize;

        public ObjectPoolAsync(int initialCount, Func<T> factory, CancellationToken? cts = null, int msecTimeout = 0)
        {
            this.msecTimeout = msecTimeout;

            ctsToken = cts ?? new CancellationToken();
            ctsToken.Register(() => this.Dispose());

            buffer = new BufferBlock<T>(
                new DataflowBlockOptions { CancellationToken = ctsToken }
             );
            this.factory = () =>
            {
                Interlocked.Increment(ref currentSize);
                return factory();
            };
            for (int i = 0; i < initialCount; i++)
                buffer.Post(this.factory());
        }

        public int Size => currentSize;
        public Task<bool> Send(T item) => buffer.SendAsync(item);

        public Task<T> GetAsync(int timeout = 0)
        {
            var tcs = new TaskCompletionSource<T>();
            buffer.ReceiveAsync(TimeSpan.FromMilliseconds(msecTimeout))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception.InnerException is TimeoutException)
                            tcs.SetResult(factory());
                        else
                            tcs.SetException(t.Exception);
                    }
                    else if (t.IsCanceled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(t.Result);
                });
            return tcs.Task;
        }

        public void Dispose() => buffer.Complete();
    }
}
