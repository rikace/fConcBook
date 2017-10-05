using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxPublisherSubscriber.Recipes
{
    //public class RxPubSub<T> : RxPubSubMapper<T, T>
    //{
    //    public RxPubSub(ISubject<T, T> subject, Func<T, T> mapper, Func<T, bool> filter = null) : base(subject, mapper,
    //        filter)
    //    {
    //    }

    //    public RxPubSub(Func<T, T> mapper, Func<T, bool> filter = null) : base(new Subject<T>(), mapper, filter)
    //    {
    //    }
    //}

    //public class RxPubSubMapper<T, R> : IDisposable, ISubject<T, R>
    //{
    //    private ISubject<T, R> subject; //#A
    //    private readonly Func<T, bool> filter;
    //    private readonly Func<T, R> mapper;
    //    private List<IObserver<R>> observers = new List<IObserver<R>>(); //#B
    //    private List<IDisposable> observables = new List<IDisposable>(); //#C

    //    public RxPubSubMapper(ISubject<T, R> subject, Func<T, R> mapper, Func<T, bool> filter = null)
    //    {
    //        this.subject = subject; //#D
    //        this.mapper = mapper;
    //        this.filter = filter ?? new Func<T, bool>(_ => true);
    //    }

    //    public IDisposable Subscribe(IObserver<R> observer)
    //    {
    //        observers.Add(observer);
    //        subject.Subscribe(observer);
    //        return new ObserverHandler<R>(observer, observers); //#E
    //    }

    //    public IDisposable AddPublisher(IObservable<T> observable) =>
    //        observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe(subject); //#F

    //    public IObservable<R> AsObservable() =>
    //        subject.AsObservable(); //#G

    //    public void OnCompleted()
    //    {
    //        foreach (var o in observers.ToArray())
    //        {
    //            o.OnCompleted();
    //            observers.Remove(o);
    //        }
    //    }

    //    public void OnError(Exception error)
    //    {
    //        foreach (var o in observers.ToArray())
    //        {
    //            o.OnError(error);
    //            observers.Remove(o);
    //        }
    //    }

    //    public void OnNext(T value)
    //    {
    //        R newValue = default(R);
    //        try
    //        {
    //            //mapping statement
    //            newValue = mapper(value);
    //        }
    //        catch (Exception ex)
    //        {
    //            //if mapping crashed
    //            OnError(ex);
    //            return;
    //        }

    //        //if mapping succeded
    //        foreach (var o in observers)
    //            o.OnNext(newValue);
    //    }

    //    public void Dispose()
    //    {
    //        observers.ForEach(x => x.OnCompleted());
    //        observers.Clear(); //#H
    //    }
    //}

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
            private IObserver<T> observer;
            Action<IObserver<T>> onObserverLifecycleEnd;

            public ObserverHandler(IObserver<T> observer, Action<IObserver<T>> onObserverLifecycleEnd)
            {
                this.observer = observer;
                this.onObserverLifecycleEnd = onObserverLifecycleEnd;
            }

            public void Dispose()
            {
                onObserverLifecycleEnd(observer);
            }
        }
    }

    public interface IMessage
    {
        Guid Id { get; }
    }

    public interface ICommand : IMessage
    {
    }

    public interface IEvent : IMessage
    {
    }

    public sealed class Bus //: ICommandDispatcher, IEventPublisher, ISubscriber
    {
        private readonly Dictionary<Type, List<Action<IMessage>>> _actions =
            new Dictionary<Type, List<Action<IMessage>>>();

        public void Dispatch<TCommand>(TCommand command) where TCommand : ICommand
        {
            List<Action<IMessage>> handlers;
            if (_actions.TryGetValue(typeof(TCommand), out handlers))
            {
                handlers[0](command);
                foreach (var handler in handlers)
                {
                    var handler1 = handler;
                    handler1(command);
                    // Task.Factory.StartNew(x => handler1(command), handler1);
                }
            }
            else
            {
                throw new InvalidOperationException("no handler registered");
            }
            Thread.Sleep(500);
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            List<Action<IMessage>> handlers;
            if (!_actions.TryGetValue(@event.GetType(), out handlers)) return;
            foreach (var handler in handlers)
            {
                Action<IMessage> handler1 = handler;
                // handler1(@event);
                Task.Factory.StartNew(x => handler1(@event), handler1);
            }
        }

        public void RegisterHandler<T>(Action<T> handler) where T : IMessage
        {
            List<Action<IMessage>> handlers;
            if (!_actions.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Action<IMessage>>();
                _actions.Add(typeof(T), handlers);
            }
            handlers.Add(CastArgument<IMessage, T>(x => handler(x)));
        }

        public static Action<TBase> CastArgument<TBase, TDerived>(Expression<Action<TDerived>> source)
            where TDerived : TBase
        {
            if (typeof(TDerived) == typeof(TBase))
            {
                return (Action<TBase>) ((Delegate) source.Compile());
            }
            ParameterExpression sourceParameter = Expression.Parameter(typeof(TBase), "source");
            Expression<Action<TBase>> result = Expression.Lambda<Action<TBase>>(
                Expression.Invoke(
                    source,
                    Expression.Convert(sourceParameter, typeof(TDerived))),
                sourceParameter);
            return result.Compile();
        }
    }
}