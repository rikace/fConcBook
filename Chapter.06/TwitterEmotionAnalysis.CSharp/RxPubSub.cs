using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TwitterEmotionAnalysis.CSharp
{
    namespace RxPublisherSubscriber
    {
        //Listing 6.6 Reactive Publisher Subscriber in C#
        public class RxPubSub<T> : IDisposable
        {
            private readonly List<IDisposable> observables = new List<IDisposable>(); //#C
            private readonly List<IObserver<T>> observers = new List<IObserver<T>>(); //#B
            private readonly ISubject<T> subject; //#A

            public RxPubSub(ISubject<T> subject)
            {
                this.subject = subject; //#D
            }

            public RxPubSub() : this(new Subject<T>())
            {
            } //#D

            public void Dispose()
            {
                subject.OnCompleted();
                observers.ForEach(x => x.OnCompleted());
                observers.Clear(); //#H
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                observers.Add(observer);
                observables.Add(subject.Subscribe(observer));
                return new ObserverHandler<T>(observer, observers); //#E
            }

            public IDisposable AddPublisher(IObservable<T> observable)
            {
                return observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe(subject); //#F
            }

            public IObservable<T> AsObservable()
            {
                return subject.AsObservable(); //#G
            }
        }

        internal class ObserverHandler<T> : IDisposable //#I
        {
            private readonly IObserver<T> observer;
            private readonly List<IObserver<T>> observers;

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
}