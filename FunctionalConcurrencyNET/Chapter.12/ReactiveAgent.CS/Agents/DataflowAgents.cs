using Functional;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.Agents.Dataflow
{
    //Listing 12.5 An Agent in C# using TPL Dataflow
    public sealed class StatefulDataflowAgent<TState, TMessage> : IAgent<TMessage>
    {
        private TState state;
        private readonly ActionBlock<TMessage> actionBlock;

        public StatefulDataflowAgent(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action, // #A
            CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ?
                    cts.Token : CancellationToken.None  // #B
            };
            actionBlock = new ActionBlock<TMessage>(    // #C
                async msg => state = await action(state, msg), options);
        }

        public Task Send(TMessage message) => actionBlock.SendAsync(message);
        public void Post(TMessage message) => actionBlock.Post(message);

        public StatefulDataflowAgent(TState initialState, Func<TState, TMessage, TState> action, CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(
                msg => state = action(state, msg), options);
        }

        public TState State => state;
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

        public void Post(TMessage message) => actionBlock.Post(message);
        public Task Send(TMessage message) => actionBlock.SendAsync(message);
    }
}