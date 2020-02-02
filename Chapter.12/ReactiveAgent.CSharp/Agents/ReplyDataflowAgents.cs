using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Functional.CSharp.FuctionalType;
using static Functional.CSharp.FuctionalType.OptionHelpers;

namespace ReactiveAgent.CSharp
{
    //Listing 12.8  Producer/consumer using TPL Dataflow
    public sealed class StatefulReplyDataflowAgent<TState, TMessage, TReply> : // #A
        IReplyAgent<TMessage, TReply>
    {
        private readonly ActionBlock<(TMessage, // #B
            Option<TaskCompletionSource<TReply>>)> actionBlock;

        private TState state;

        public StatefulReplyDataflowAgent(TState initialState,
            Func<TState, TMessage, Task<TState>> projection,
            Func<TState, TMessage, Task<(TState, TReply)>> ask,
            CancellationTokenSource cts = null) // #C
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };
            actionBlock = new ActionBlock<(TMessage, Option<TaskCompletionSource<TReply>>)>(
                async message =>
                {
                    var (msg, replyOpt) = message;
                    await replyOpt.Match( // #D
                        async () => state = await projection(state, msg), // #E
                        async reply =>
                        {
                            // #F
                            var (newState, replyresult) = await ask(state, msg);
                            state = newState;
                            reply.SetResult(replyresult);
                        });
                }, options);
        }

        public StatefulReplyDataflowAgent(TState initialState,
            Func<TState, TMessage, TState> projection,
            Func<TState, TMessage, (TState, TReply)> ask,
            CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };
            actionBlock = new ActionBlock<(TMessage, Option<TaskCompletionSource<TReply>>)>(
                message =>
                {
                    var (msg, replyOpt) = message;
                    replyOpt.Match(() => state = projection(state, msg),
                        reply =>
                        {
                            var (newState, replyresult) = ask(state, msg);
                            state = newState;
                            reply.SetResult(replyresult);
                            return state;
                        });
                }, options);
        }

        public Task<TReply> Ask(TMessage message)
        {
            var tcs = new TaskCompletionSource<TReply>(); // #G
            actionBlock.Post((message, Some(tcs)));
            return tcs.Task; // #G
        }

        public Task Send(TMessage message)
        {
            return actionBlock.SendAsync((message, None));
        }

        public void Post(TMessage message)
        {
            actionBlock.Post((message, None));
        }
    }
}