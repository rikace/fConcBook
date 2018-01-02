using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelRecipes.Aggregator
{
    class Program
    {
        internal class MyMessage : IMessage
        {
            public string Value { get; set; }
        }

        internal class MyMessage2 : IMessage
        {
            public int number { get; set; }
        }

        static void Main(string[] args)
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


            eventAggregator.Publish(new MyMessage { Value = "Hello" });
            eventAggregator.Publish(new MyMessage2 { number = 42 });

            eventAggregator.Publish(new MyMessage { Value = "World" });
            eventAggregator.Publish(new MyMessage2 { number = 77 });

            Console.ReadLine();
        }
    }
}