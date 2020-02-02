using System;

namespace EventAggregator.CSharp
{
    public interface IMessage
    {
    }

    public interface ISubscription<in TMessage> : IDisposable where TMessage : IMessage
    {
        Action<TMessage> Action { get; }
        IEventAggregator EventAggregator { get; }
    }
}