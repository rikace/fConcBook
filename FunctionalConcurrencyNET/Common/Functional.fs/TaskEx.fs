namespace FunctionalConcurrency

module TaskEx =

    open System
    open System.Threading
    open System.Threading.Tasks
    open Utilities

    /// Task result
    type TaskResult<'T> =
        /// Task was canceled
        | Canceled
        /// Unhandled exception in task
        | Error of exn
        /// Task completed successfully
        | Successful of 'T

    let run (t: unit -> Task<_>) =
        try
            let task = t()
            task.Result |> TaskResult.Successful
        with
        | :? OperationCanceledException -> TaskResult.Canceled
        | :? AggregateException as e ->
            match e.InnerException with
            | :? TaskCanceledException -> TaskResult.Canceled
            | _ -> TaskResult.Error e
        | e -> TaskResult.Error e

    let toAsync (t: Task<'T>): Async<'T> =
        let abegin (cb: AsyncCallback, state: obj) : IAsyncResult =
            match cb with
            | null -> upcast t
            | cb ->
                t.ContinueWith(fun (_ : Task<_>) -> cb.Invoke t) |> ignore
                upcast t
        let aend (r: IAsyncResult) =
            (r :?> Task<'T>).Result
        Async.FromBeginEnd(abegin, aend)

    /// Transforms a Task's first value by using a specified mapping function.
    let inline map f (m: Task<_>) =
        m.ContinueWith(fun (t: Task<_>) -> f t.Result)

    let inline bindWithOptions (token: CancellationToken) (continuationOptions: TaskContinuationOptions) (scheduler: TaskScheduler) (f: 'T -> Task<'U>) (m: Task<'T>) =
        m.ContinueWith((fun (x: Task<_>) -> f x.Result), token, continuationOptions, scheduler).Unwrap()

    let inline bind (f: 'T -> Task<'U>) (m: Task<'T>) =
        m.ContinueWith(fun (x: Task<_>) -> f x.Result).Unwrap()

    let inline returnM a =
        let s = TaskCompletionSource()
        s.SetResult a
        s.Task

    /// Sequentially compose two actions, passing any value produced by the first as an argument to the second.
    let inline (>>=) m f = bind f m

    /// Flipped >>=
    let inline (=<<) f m = bind f m

    /// Left-to-right Kleisli composition
    let inline (>=>) f g = fun x -> f x >>= g

    /// Right-to-left Kleisli composition
    let inline (<=<) x = flip (>=>) x

    /// Promote a function to a monad/applicative, scanning the monadic/applicative arguments from left to right.
    let inline lift2 f a b =
        a >>= fun aa -> b >>= fun bb -> f aa bb |> returnM

    /// Sequential application
    let inline apply x f = lift2 id f x

    /// Sequential application
    let inline (<*>) f x = apply x f

    /// Infix map
    let inline (<!>) f x = map f x

    /// Sequence actions, discarding the value of the first argument.
    let inline ( *>) a b = lift2 (fun _ z -> z) a b

    /// Sequence actions, discarding the value of the second argument.
    let inline ( <*) a b = lift2 (fun z _ -> z) a b


