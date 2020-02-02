using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.CSharp
{
    //Listing 12.5 An Agent in C# using TPL Dataflow
    public sealed class StatefulDataflowAgent<TState, TMessage> : IAgent<TMessage>
    {
        private readonly ActionBlock<TMessage> actionBlock;

        public StatefulDataflowAgent(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action, // #A
            CancellationTokenSource cts = null)
        {
            State = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None // #B
            };
            actionBlock = new ActionBlock<TMessage>( // #C
                async msg => State = await action(State, msg), options);
        }

        public StatefulDataflowAgent(TState initialState, Func<TState, TMessage, TState> action,
            CancellationTokenSource cts = null)
        {
            State = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(
                msg => State = action(State, msg), options);
        }

        public TState State { get; private set; }

        public Task Send(TMessage message)
        {
            return actionBlock.SendAsync(message);
        }

        public void Post(TMessage message)
        {
            actionBlock.Post(message);
        }
    }

    public sealed class StatelessDataflowAgent<TMessage> : IAgent<TMessage>
    {
        private readonly ActionBlock<TMessage> actionBlock;

        public StatelessDataflowAgent(Action<TMessage> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public StatelessDataflowAgent(Func<TMessage, Task> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public void Post(TMessage message)
        {
            actionBlock.Post(message);
        }

        public Task Send(TMessage message)
        {
            return actionBlock.SendAsync(message);
        }
    }
}