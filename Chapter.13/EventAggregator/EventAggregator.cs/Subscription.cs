using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelRecipes.Aggregator
{
    public class Subscription<TMessage> : ISubscription<TMessage> where TMessage : IMessage
    {
        public Subscription(IEventAggregator eventAggregator, Action<TMessage> action)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException("eventAggregator");
            Action = action ?? throw new ArgumentNullException("action");
        }

        public Action<TMessage> Action { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }

        public void Dispose()
        {
            EventAggregator.UnSubscribe(this);
            GC.SuppressFinalize(this);
        }
    }
}
