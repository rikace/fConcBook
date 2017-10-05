using System;
using System.Reactive.Linq;

namespace RxBus
{

    //var subscriptionService = new SubscriptionService( IBus> )
    //subscriptionService.Subscribe( Predicate<object , null);
    public sealed class SubscriptionService : ISubscriptionService
    {
        readonly IObservable<object> _observable;

        public SubscriptionService(IObservable<object> observable)
        {
            if (observable == null) throw new ArgumentNullException("observable");
            _observable = observable;
        }

        // Subscribes the given handler to the message bus. Only messages for which the given predicate resolves to true will be passed to the handler.
        public IDisposable Subscribe<TMessage>(Predicate<TMessage> canHandle, Action<TMessage> handle)
        {
            if (canHandle == null) throw new ArgumentNullException("canHandle");
            if (handle == null) throw new ArgumentNullException("handle");
            // TODO: figure out how to unit test this
            return _observable.OfType<TMessage>().Where(msg => canHandle(msg)).Subscribe(handle);
        }
    }
}