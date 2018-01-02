using Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Functional.OptionHelpers;

namespace ReactiveAgent.Agents.Dataflow
{
    //Listing 12.8  Producer/consumer using TPL Dataflow
    public sealed class StatefulReplyDataflowAgent<TState, TMessage, TReply> : // #A
                                            IReplyAgent<TMessage, TReply>
    {
        private TState state;
        private readonly ActionBlock<(TMessage,     // #B
                                      Option<TaskCompletionSource<TReply>>)> actionBlock;

        public StatefulReplyDataflowAgent(TState initialState,
                    Func<TState, TMessage, Task<TState>> projection,
                    Func<TState, TMessage, Task<(TState, TReply)>> ask,
                    CancellationTokenSource cts = null)   // #C
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };
            actionBlock = new ActionBlock<(TMessage, Option<TaskCompletionSource<TReply>>)>(
              async message => {
                  (TMessage msg, Option<TaskCompletionSource<TReply>> replyOpt) = message;
                  await replyOpt.Match(     // #D
                          none: async () => state = await projection(state, msg), // #E
                          some: async reply => {        // #F
                              (TState newState, TReply replyresult) = await ask(state, msg);
                              state = newState;
                              reply.SetResult(replyresult);
                          });
              }, options);
        }

        public Task<TReply> Ask(TMessage message)
        {
            var tcs = new TaskCompletionSource<TReply>();  // #G
            actionBlock.Post((message, Some(tcs)));
            return tcs.Task;	// #G
        }

        public Task Send(TMessage message) =>
            actionBlock.SendAsync((message, None));

        public void Post(TMessage message) =>
            actionBlock.Post((message, None));

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
              message => {
                  (TMessage msg, Option<TaskCompletionSource<TReply>> replyOpt) = message;
                  replyOpt.Match(none: () => (state = projection(state, msg)),
                                 some: reply =>
                                 {
                                     (TState newState, TReply replyresult) = ask(state, msg);
                                     state = newState;
                                     reply.SetResult(replyresult);
                                     return state;
                                 });
              }, options);
        }
    }
}