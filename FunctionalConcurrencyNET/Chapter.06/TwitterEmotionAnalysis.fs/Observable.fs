module Observable

    open System
    open System.Reactive.Linq

    let delay (span : TimeSpan) (observable : IObservable<'T>) =
        observable.Delay(span)

    let throttle (span : TimeSpan) (observable : IObservable<'T>) =
        observable.Throttle(span)

    let groupBy (selector) (observable : IObservable<'T>) =
        observable.GroupBy(Func<'T, 'R>(selector))

    let bufferSpan (span : TimeSpan) (observable : IObservable<'T>) =
        observable.Buffer(span)

    let bufferCount (count : int) (observable : IObservable<'T>) =
        observable.Buffer(count)

    let bufferCountTimer (span : TimeSpan, count : int) (observable : IObservable<'T>) =
        observable.Buffer(span, count)

    let selectMany (selector) (observable : IObservable<'T>) =
        observable.SelectMany(Func<'T, IObservable<'R>>(selector))

    let action f (observable : IObservable<'T>) =
        observable.Do(Action<'T>(f))