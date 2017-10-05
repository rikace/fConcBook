using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelRecipes.Aggregator
{
    internal class TestAggregator
    {
        public static void Start()
        {
            var eventAggregator = new EventAggregator();
            Action<MyMessage> someAction = message => Console.WriteLine(message.Value);
            ISubscription<MyMessage> subscription1 = eventAggregator.Subscribe(someAction);
            ISubscription<MyMessage> subscription2 =
                eventAggregator.Subscribe<MyMessage>(message => Console.WriteLine("Test 2 - {0}", message.Value));

            Action<MyMessage2> someAction2 = message => Console.WriteLine(message.number);

            ISubscription<MyMessage2> subscription3 = eventAggregator.Subscribe(someAction2);

            ISubscription<MyMessage2> subscription4 =
                eventAggregator.Subscribe<MyMessage2>(message => Console.WriteLine("Test Number - {0}", message.number));


            eventAggregator.Publish(new MyMessage { Value = "ciao" });
            eventAggregator.Publish(new MyMessage2 { number = 7 });
            eventAggregator.Publish(new MyMessage { Value = "Bugghina" });
            eventAggregator.Publish(new MyMessage2 { number = "Bugghina".Length });
        }

        internal class MyMessage : IMessage
        {
            public string Value { get; set; }
        }

        internal class MyMessage2 : IMessage
        {
            public int number { get; set; }
        }
    }

    public interface IMessage
    {
    }

    public interface IEventAggregator
    {
        void Publish<TMessage>(TMessage message) where TMessage : IMessage;

        ISubscription<TMessage> Subscribe<TMessage>(Action<TMessage> action) where TMessage : IMessage;

        void UnSubscribe<TMessage>(ISubscription<TMessage> subscription) where TMessage : IMessage;

        void ClearAllSubscriptions();
        void ClearAllSubscriptions(Type[] exceptMessages);
    }

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


    public interface ISubscription<in TMessage> : IDisposable where TMessage : IMessage
    {
        Action<TMessage> Action { get; }
        IEventAggregator EventAggregator { get; }
    }

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