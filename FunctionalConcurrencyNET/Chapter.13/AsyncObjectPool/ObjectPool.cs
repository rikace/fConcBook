using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowObjectPoolEncryption
{
    // Listing 13.1 Asynchronous Object-Pool
    public sealed class ObjectPoolAsync<T> : IDisposable
    {
        private readonly BufferBlock<T> buffer;
        private readonly Func<T> factory;
        private readonly int msecTimeout;
        private int currentSize;

        public ObjectPoolAsync(int initialCount, Func<T> factory, CancellationToken? cts = null, int msecTimeout = 0)
        {
            this.msecTimeout = msecTimeout;

            var ctsToken = cts ?? new CancellationToken();
            ctsToken.Register(Dispose);

            buffer = new BufferBlock<T>( // #A
                new DataflowBlockOptions { CancellationToken = ctsToken }
            );
            this.factory = () =>
            {
                Interlocked.Increment(ref currentSize);
                return factory();
            };
            for (int i = 0; i < initialCount; i++)
                buffer.Post(this.factory()); // #B
        }

        public int Size => currentSize;
        public Task<bool> PutAsync(T item) => buffer.SendAsync(item); // #C

        public Task<T> GetAsync(int timeout = 0) // #D
        {
            var tcs = new TaskCompletionSource<T>();
            buffer.ReceiveAsync(TimeSpan.FromMilliseconds(msecTimeout))
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception.InnerException is TimeoutException)
                            tcs.SetResult(factory());
                        else
                            tcs.SetException(task.Exception);
                    }
                    else if (task.IsCanceled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(task.Result);
                });
            return tcs.Task;
        }

        public void Dispose() => buffer.Complete();
    }
}
