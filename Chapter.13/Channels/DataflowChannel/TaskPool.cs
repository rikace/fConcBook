using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace DataflowChannel
{
    public class Context
    {
        public Context(Action action, ExecutionContext context)
        {
            Action = action;
            ExecutionContext = context;
        }

        public ExecutionContext ExecutionContext { get; }
        public Action Action { get; }
    }

    public class TaskPool : IDisposable
    {
        public static TaskPool Instance = new TaskPool(4);
        private readonly ActionBlock<Context> actionBlock;

        private readonly CancellationTokenSource cts;

        private TaskPool(int maxDegreeOfParallelism)
        {
            cts = new CancellationTokenSource();
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            actionBlock = new ActionBlock<Context>(ctx =>
            {
                var ec = ctx.ExecutionContext.CreateCopy();
                ExecutionContext.Run(ec, x => ctx.Action(), null);
            }, options);
        }

        public void Dispose()
        {
            actionBlock.Complete();
        }

        public void Add(Action action, ExecutionContext ec)
        {
            ec = ec ?? ExecutionContext.Capture();
            var ctx = new Context(action, ec);
            actionBlock.Post(ctx);
        }

        public static void Spawn(Action action, ExecutionContext ec)
        {
            Instance.Add(action, ec);
        }

        public static void Stop()
        {
            Instance.Dispose();
        }
    }
}