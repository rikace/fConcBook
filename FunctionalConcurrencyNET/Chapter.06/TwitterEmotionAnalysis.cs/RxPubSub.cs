using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RxPublisherSubscriber
{
    //Listing 6.6 Reactive Publisher Subscriber in C#
    public class RxPubSub<T> : IDisposable
    {
        private ISubject<T> subject; //#A
        private List<IObserver<T>> observers = new List<IObserver<T>>(); //#B
        private List<IDisposable> observables = new List<IDisposable>(); //#C

        public RxPubSub(ISubject<T> subject)
        {
            this.subject = subject; //#D
        }
        public RxPubSub() : this(new Subject<T>()) { } //#D

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observers.Add(observer);
            subject.Subscribe(observer);
            return new ObserverHandler<T>(observer, observers); //#E
        }

        public IDisposable AddPublisher(IObservable<T> observable) =>
            observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe(subject); //#F

        public IObservable<T> AsObservable() => subject.AsObservable(); //#G
        public void Dispose()
        {
            observers.ForEach(x => x.OnCompleted());
            observers.Clear(); //#H
        }
    }

    class ObserverHandler<T> : IDisposable //#I
    {
        private IObserver<T> observer;
        private List<IObserver<T>> observers;

        public ObserverHandler(IObserver<T> observer, List<IObserver<T>> observers)
        {
            this.observer = observer;
            this.observers = observers;
        }

        public void Dispose() //#I
        {
            observer.OnCompleted();
            observers.Remove(observer);
        }
    }
}
