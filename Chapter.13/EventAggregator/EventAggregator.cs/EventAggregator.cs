using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelRecipes.Aggregator
{
    public class EventAggregator : IEventAggregator
    {
        private readonly IDictionary<Type, IList> _subscriptions = new Dictionary<Type, IList>();

        public void Publish<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null) throw new ArgumentNullException("message");

            Type messageType = typeof(TMessage);
            if (_subscriptions.ContainsKey(messageType))
            {
                var subscriptionList =
                    new List<ISubscription<TMessage>>(_subscriptions[messageType].OfType<ISubscription<TMessage>>());
                foreach (var subscription in subscriptionList)
                    subscription.Action(message);
            }
        }

        public ISubscription<TMessage> Subscribe<TMessage>(Action<TMessage> action) where TMessage : IMessage
        {
            Type messageType = typeof(TMessage);
            var subscription = new Subscription<TMessage>(this, action);

            if (_subscriptions.ContainsKey(messageType))
                _subscriptions[messageType].Add(subscription);
            else
                _subscriptions.Add(messageType, new List<ISubscription<TMessage>> { subscription });

            return subscription;
        }

        public void UnSubscribe<TMessage>(ISubscription<TMessage> subscription) where TMessage : IMessage
        {
            Type messageType = typeof(TMessage);
            if (_subscriptions.ContainsKey(messageType))
                _subscriptions[messageType].Remove(subscription);
        }

        public void ClearAllSubscriptions()
        {
            ClearAllSubscriptions(null);
        }

        public void ClearAllSubscriptions(Type[] exceptMessages)
        {
            foreach (var messageSubscriptions in new Dictionary<Type, IList>(_subscriptions))
            {
                bool canDelete = true;
                if (exceptMessages != null)
                    canDelete = !exceptMessages.Contains(messageSubscriptions.Key);

                if (canDelete)
                    _subscriptions.Remove(messageSubscriptions);
            }
        }
    }
}