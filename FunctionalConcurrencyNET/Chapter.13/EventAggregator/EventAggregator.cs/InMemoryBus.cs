using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace RxBus
{
    //using (var bus = new InMemoryBus()) {
    //    var publisher = new Subject<object>();
    //bus.AddPublisher(publisher);

    //    var messagesPublishedByBus = new List<object>();
    //bus.Subscribe(messagesPublishedByBus.Add);

    //    var message = new object();
    //publisher.OnNext(message);


    // Implementation of IBus that keeps publishers and subscriptions in memory.
    public sealed class InMemoryBus : IBus
    {
        readonly Subject<object> _subject;
        readonly List<IDisposable> _publisherSubscriptions;

        public InMemoryBus()
        {
            _subject = new Subject<object>();
            _publisherSubscriptions = new List<IDisposable>();
        }


        /// Adds the given IObservable as a message source.
        public void AddPublisher(IObservable<object> observable)
        {
            if (observable == null) throw new ArgumentNullException("observable");
            _publisherSubscriptions.Add(observable.Subscribe(msg => _subject.OnNext(msg)));
        }

        // Notifies the provider that an observer is to receive notifications.
        public IDisposable Subscribe(IObserver<object> observer)
        {
            return _subject.Subscribe(observer);
        }

        bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _publisherSubscriptions.ForEach(d => d.Dispose());
                _subject.Dispose();
            }
            _disposed = true;
        }
    }
}