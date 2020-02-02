using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveAgent.CSharp
{
    public interface IAgent<TMessage>
    {
        Task Send(TMessage message);
        void Post(TMessage message);
    }

    public interface IReplyAgent<TMessage, TReply> : IAgent<TMessage>
    {
        Task<TReply> Ask(TMessage message);
    }

    public static class Agent
    {
        public static IAgent<TMessage> Start<TMessage>(Action<TMessage> action, CancellationTokenSource cts = null)
        {
            return new StatelessDataflowAgent<TMessage>(action, cts);
        }

        public static IAgent<TMessage> Start<TMessage>(Func<TMessage, Task> action, CancellationTokenSource cts = null)
        {
            return new StatelessDataflowAgent<TMessage>(action, cts);
        }


        public static IAgent<TMessage> Start<TState, TMessage>(TState initialState,
            Func<TState, TMessage, Task<TState>> action, CancellationTokenSource cts = null)
        {
            return new StatefulDataflowAgent<TState, TMessage>(initialState, action, cts);
        }

        public static IAgent<TMessage> Start<TState, TMessage>(TState initialState,
            Func<TState, TMessage, TState> action, CancellationTokenSource cts = null)
        {
            return new StatefulDataflowAgent<TState, TMessage>(initialState, action, cts);
        }


        public static IReplyAgent<TMessage, TReply> Start<TState, TMessage, TReply>(TState initialState,
            Func<TState, TMessage, Task<TState>> projection, Func<TState, TMessage, Task<(TState, TReply)>> ask,
            CancellationTokenSource cts = null)
        {
            return new StatefulReplyDataflowAgent<TState, TMessage, TReply>(initialState, projection, ask, cts);
        }

        public static IReplyAgent<TMessage, TReply> Start<TState, TMessage, TReply>(TState initialState,
            Func<TState, TMessage, TState> projection, Func<TState, TMessage, (TState, TReply)> ask,
            CancellationTokenSource cts = null)
        {
            return new StatefulReplyDataflowAgent<TState, TMessage, TReply>(initialState, projection, ask, cts);
        }
    }
}