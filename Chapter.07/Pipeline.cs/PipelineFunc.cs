using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Functional;
using static Pipeline.PipelineFunc;
using Unit = Microsoft.FSharp.Core.Unit;

namespace PipelineFunc
{
    public class CsPipeline<TInput, TOutput> : IPipeline<TInput, TOutput>
    {
        private struct Continuation
        {
            public Func<Tuple<TInput, TOutput>, Unit> Callback;
            public TInput Input;
        }

        private readonly Func<TInput, TOutput> _function;
        private BlockingCollection<Continuation>[] _continuations;

        public CsPipeline(Func<TInput, TOutput> function)
        {
            _function = function;
        }

        public IPipeline<TInput, TMapped> Then<TMapped>(Func<TOutput, TMapped> nextfunction)
        {
            Func<TInput, TMapped> compose = _function.Compose(nextfunction);
            return new CsPipeline<TInput, TMapped>(compose);
        }

        public void Enqueue(TInput input, Func<Tuple<TInput, TOutput>, Unit> callback)
            => BlockingCollection<Continuation>.TryAddToAny(_continuations,
                new Continuation
                {
                    Input = input,
                    Callback = callback
                });

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
                        {
                            continuation.Callback.Invoke(
                                Tuple.Create(continuation.Input, _function.Invoke(continuation.Input)));
                        }
                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);


            return new StopPipelineDisposable(this);
        }

        private class StopPipelineDisposable : IDisposable
        {
            readonly CsPipeline<TInput, TOutput> _pipeline;

            public StopPipelineDisposable(CsPipeline<TInput, TOutput> pipeline)
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