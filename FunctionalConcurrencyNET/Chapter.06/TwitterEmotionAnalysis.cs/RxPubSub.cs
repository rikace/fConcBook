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
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>(); //#B
        // TODO
        private readonly List<IDisposable> observables = new List<IDisposable>(); //#C

        public RxPubSub(ISubject<T> subject)
        {
            this.subject = subject; //#D
        }
        public RxPubSub() : this(new Subject<T>()) { } //#D

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observers.Add(observer);
            observables.Add(this.subject.Subscribe(observer));
            return new ObserverHandler<T>(observer, observers); //#E
        }

        public IDisposable AddPublisher(IObservable<T> observable) =>
            observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe(subject); //#F

        public IObservable<T> AsObservable() => subject.AsObservable(); //#G
        public void Dispose()
        {
            subject.OnCompleted();
            observers.ForEach(x => x.OnCompleted());
            observers.Clear(); //#H
        }
    }

    class ObserverHandler<T> : IDisposable //#I
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

// TODO
public sealed class MapperSubject<Tin, Tout> : ISubject<Tin, Tout>
{
    readonly Func<Tin, Tout> mapper;
    public MapperSubject(Func<Tin, Tout> mapper)
    {
        this.mapper = mapper;
    }

    public void OnCompleted()
    {
        foreach (var o in observers.ToArray())
        {
            o.OnCompleted();
            observers.Remove(o);
        }
    }

    public void OnError(Exception error)
    {
        foreach (var o in observers.ToArray())
        {
            o.OnError(error);
            observers.Remove(o);
        }
    }

    public void OnNext(Tin value)
    {
        Tout newValue = default(Tout);
        try
        {
            //mapping statement
            newValue = mapper(value);
        }
        catch (Exception ex)
        {
            //if mapping crashed
            OnError(ex);
            return;
        }

        //if mapping succeded
        foreach (var o in observers)
            o.OnNext(newValue);
    }

    //all registered observers
    private readonly List<IObserver<Tout>> observers = new List<IObserver<Tout>>();
    public IDisposable Subscribe(IObserver<Tout> observer)
    {
        observers.Add(observer);
        return new ObserverHandler<Tout>(observer, OnObserverLifecycleEnd);
    }

    private void OnObserverLifecycleEnd(IObserver<Tout> o)
    {
        o.OnCompleted();
        observers.Remove(o);
    }

    //this class simply informs the subject that a dispose
    //has been invoked against the observer causing its removal
    //from the observer collection of the subject
    private class ObserverHandler<T> : IDisposable
    {
        private readonly IObserver<T> observer;
        readonly Action<IObserver<T>> onObserverLifecycleEnd;
        public ObserverHandler(IObserver<T> observer, Action<IObserver<T>> onObserverLifecycleEnd)
        {
            this.observer = observer;
            this.onObserverLifecycleEnd = onObserverLifecycleEnd;
        }

        public void Dispose() =>
            onObserverLifecycleEnd(observer);
    }
}
