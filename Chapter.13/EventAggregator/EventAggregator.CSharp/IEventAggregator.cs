using System;

namespace EventAggregator.CSharp
{
    public interface IEventAggregator
    {
        void Publish<TMessage>(TMessage message) where TMessage : IMessage;

        ISubscription<TMessage> Subscribe<TMessage>(Action<TMessage> action) where TMessage : IMessage;

        void UnSubscribe<TMessage>(ISubscription<TMessage> subscription) where TMessage : IMessage;

        void ClearAllSubscriptions();
        void ClearAllSubscriptions(Type[] exceptMessages);
    }
}