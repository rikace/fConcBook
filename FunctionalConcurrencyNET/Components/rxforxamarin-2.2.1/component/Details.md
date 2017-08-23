Reactive Extensions (Rx) is a library for composing asynchronous
and event-based programs using observable sequences and LINQ-style
query operators. Using Rx, developers represent asynchronous data
streams with `Observables`, query asynchronous data streams using LINQ
operators, and parameterize the concurrency in the asynchronous data
streams using `Schedulers`. Simply put, Rx = Observables + LINQ +
Schedulers.

Whether you are authoring a traditional desktop or web-based
application, you have to deal with asynchronous and event-based
programming from time to time. Desktop applications have I/O
operations and computationally expensive tasks that might take a long
time to complete and potentially block other active
threads. Furthermore, handling exceptions, cancellation, and
synchronization is difficult and error-prone.

Using Rx, you can represent multiple asynchronous data streams that
come from diverse sources, e.g., stock quotes, tweets, computer events,
web service requests, etc., and subscribe to the event stream using
the `IObserver<T>` interface. The `IObservable<T>` interface notifies the
subscribed `IObserver<T>` interface whenever an event occurs.

Because observable sequences are data streams, you can query them
using standard LINQ query operators implemented by the Observable
extension methods. Thus you can filter, project, aggregate, compose
and perform time-based operations on multiple events easily by using
these standard LINQ operators. In addition, there are a number of
other reactive stream specific operators that allow powerful queries
to be written.  Cancellation, exceptions, and synchronization are also
handled gracefully by using the extension methods provided by Rx.

To learn more about the Reactive Extensions, see Microsoft's 
RX open source site:

   http://rx.codeplex.com

## Current Version

*	current version supports 
	*	Rx.net v.2.2
	*	iOS Unified and Classic API
	