using System;

namespace EventAggregator.CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var eventAggregator = new EventAggregator();
            Action<MyMessage> someAction = message => Console.WriteLine(message.Value);
            var subscription1 = eventAggregator.Subscribe(someAction);
            var subscription2 =
                eventAggregator.Subscribe<MyMessage>(message => Console.WriteLine("Test 2 - {0}", message.Value));

            Action<MyMessage2> someAction2 = message => Console.WriteLine(message.number);

            var subscription3 = eventAggregator.Subscribe(someAction2);

            var subscription4 =
                eventAggregator.Subscribe<MyMessage2>(message =>
                    Console.WriteLine("Test Number - {0}", message.number));


            eventAggregator.Publish(new MyMessage {Value = "Hello"});
            eventAggregator.Publish(new MyMessage2 {number = 42});

            eventAggregator.Publish(new MyMessage {Value = "World"});
            eventAggregator.Publish(new MyMessage2 {number = 77});

            Console.ReadLine();
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
}