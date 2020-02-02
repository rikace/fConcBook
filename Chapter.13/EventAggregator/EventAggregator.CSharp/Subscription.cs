using System;

namespace EventAggregator.CSharp
{
    public class Subscription<TMessage> : ISubscription<TMessage> where TMessage : IMessage
    {
        public Subscription(IEventAggregator eventAggregator, Action<TMessage> action)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException("eventAggregator");
            Action = action ?? throw new ArgumentNullException("action");
        }

        public Action<TMessage> Action { get; }
        public IEventAggregator EventAggregator { get; }

        public void Dispose()
        {
            EventAggregator.UnSubscribe(this);
            GC.SuppressFinalize(this);
        }
    }
}