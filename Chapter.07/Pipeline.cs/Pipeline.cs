using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;

namespace Pipeline
{
    public class CsPipeline<TInput, TOutput> : IPipeline<TInput, TOutput>
    {
        private struct Continuation
        {
            public FSharpFunc<Tuple<TInput, TOutput>, Unit> Callback;
            public TInput Input;
        }

        private readonly FSharpFunc<TInput, TOutput> _function;
        private BlockingCollection<Continuation>[] _continuations;

        public CsPipeline(FSharpFunc<TInput, TOutput> function)
        {
            _function = function;
        }

        public IPipeline<TInput, TMapped> Then<TMapped>(FSharpFunc<TOutput, TMapped> nextfunction)
        {
            var compose =
                FSharpFuncUtils.Create<TInput, TMapped>(value =>
                    nextfunction.Invoke(_function.Invoke(value)));
            return new CsPipeline<TInput, TMapped>(compose);
        }

        public void Enqueue(TInput input, FSharpFunc<Tuple<TInput, TOutput>, Unit> callback)
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
                System.Threading.Tasks.Task.Factory.StartNew(() =>
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