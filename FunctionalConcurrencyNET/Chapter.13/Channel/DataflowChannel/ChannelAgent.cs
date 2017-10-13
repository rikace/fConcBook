using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveAgent.Agents.Dataflow;
using System.Threading;

namespace DataflowChannel
{
    public interface IChannelMsg<T> { }
    public struct Recv<T> : IChannelMsg<T>
    {
        public Recv(Action<T> recv, ExecutionContext ec =null) {
            Action = recv;
            ExecutionContext = ec ?? ExecutionContext.Capture();
        }
        public Action<T> Action { get; }
        public ExecutionContext ExecutionContext { get; }
    }
    public struct Send<T> : IChannelMsg<T>
    {
        public Send(T value)
        {
            Value = value;
        }
        public T Value { get; }
    }

    public class ChannelAgent<T>
    {
        public ChannelAgent(CancellationTokenSource cts = null)
        {
            agent =
                new StatefulDataflowAgent<(Queue<T>, Queue<Recv<T>>), IChannelMsg<T>>(
                    (new Queue<T>(), new Queue<Recv<T>>()),
                    (state, message) =>
                    {
                        var (writers, readers) = state;
                        switch (message)
                        {
                            case Recv<T> msg:
                                if (writers.Count == 0)
                                    readers.Enqueue(msg);
                                else
                                    TaskPool.Spawn(() =>
                                        msg.Action(writers.Dequeue()), msg.ExecutionContext);
                                break;
                            case Send<T> msg:
                                if (readers.Count == 0)
                                    writers.Enqueue(msg.Value);
                                else
                                {
                                    var recv = readers.Dequeue();
                                    TaskPool.Spawn(() =>
                                        recv.Action(msg.Value), recv.ExecutionContext);
                                }
                                break;
                            default:
                                throw new Exception("Unknown message");
                        }
                        return state;
                    }, cts);
        }

        private StatefulDataflowAgent<(Queue<T>, Queue<Recv<T>>), IChannelMsg<T>> agent;

        private Task Recv(Action<T> handler)
            => agent.Send(new Recv<T>(handler));

        public Task Send(T value)
            => agent.Send(new Send<T>(value));

        public Task Subscribe(Action<T> handler)
            => Task.Run(async () =>
            {
                while (true)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    await Recv(x =>
                    {
                        handler(x);
                        tcs.SetResult(true);
                    });
                    await tcs.Task;
                }
            });
    }
}
