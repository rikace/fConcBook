using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Functional.CSharp;
using Microsoft.FSharp.Core;
using static Pipeline.FSharp.PipelineFunc;

namespace Pipeline.CSharp
{
    public class CsPipelineFunc<TInput, TOutput> : IPipeline<TInput, TOutput>
    {
        private readonly Func<TInput, TOutput> _function;
        private BlockingCollection<Continuation>[] _continuations;

        public CsPipelineFunc(Func<TInput, TOutput> function)
        {
            _function = function;
        }

        public IPipeline<TInput, TMapped> Then<TMapped>(Func<TOutput, TMapped> nextfunction)
        {
            var compose = _function.Compose(nextfunction);
            return new CsPipelineFunc<TInput, TMapped>(compose);
        }

        public void Enqueue(TInput input, Func<Tuple<TInput, TOutput>, Unit> callback)
        {
            BlockingCollection<Continuation>.TryAddToAny(_continuations,
                new Continuation
                {
                    Input = input,
                    Callback = callback
                });
        }

        public void Stop()
        {
            foreach (var bc in _continuations)
                bc.CompleteAdding();
        }

        public IDisposable Execute(int blockingCollectionPoolSize, CancellationToken cancellationToken)
        {
            _continuations =
                Enumerable.Range(0, blockingCollectionPoolSize)
                    .Select(_ => new BlockingCollection<Continuation>(100))
                    .ToArray();

            cancellationToken.Register(Stop);

            for (var x = 0; x < blockingCollectionPoolSize; x++)
                Task.Factory.StartNew(() =>
                {
                    while (!_continuations.All(bc => bc.IsCompleted) && !cancellationToken.IsCancellationRequested)
                    {
                        Continuation continuation;
                        if (BlockingCollection<Continuation>.TryTakeFromAny(_continuations, out continuation) >= 0)
                            continuation.Callback.Invoke(
                                Tuple.Create(continuation.Input, _function.Invoke(continuation.Input)));
                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);


            return new StopPipelineDisposable(this);
        }

        private struct Continuation
        {
            public Func<Tuple<TInput, TOutput>, Unit> Callback;
            public TInput Input;
        }

        private class StopPipelineDisposable : IDisposable
        {
            private readonly CsPipelineFunc<TInput, TOutput> _pipeline;

            public StopPipelineDisposable(CsPipelineFunc<TInput, TOutput> pipeline)
            {
                _pipeline = pipeline;
            }

            public void Dispose()
            {
                _pipeline.Stop();
            }
        }
    }
}