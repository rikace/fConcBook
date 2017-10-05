using System;

namespace RxBus
{
    // Subscribes an action to the message bus.
    public interface ISubscriptionService
    {
        // Subscribes the given handler to the message bus. Only messages for which the given predicate resolves to true will be passed to the handler.
        IDisposable Subscribe<TMessage>(Predicate<TMessage> canHandle, Action<TMessage> handle);
    }
}