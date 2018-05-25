namespace FunctionalConcurrency

open System
open System.IO
open System.Threading.Tasks
open System.Threading

[<AutoOpen>]
module AsyncHelpers =

    // Listing 9.9 Extending the Asynchronous-Workflow to support Task<’a>
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t : Task<'T>, f : 'T -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)

        member x.Bind(t : Task, f : unit -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task) = x.ReturnFrom(Async.AwaitTask computation)

        member this.Using(disp:#System.IDisposable, (f:Task<'T> -> Async<'R>)) : Async<'R> =
            this.TryFinally(f disp, fun () ->
                match disp with
                    | null -> ()
                    | disp -> disp.Dispose())

    type Microsoft.FSharp.Control.Async<'a> with
        static member Parallel2(a : Async<'a>, b : Async<'b>) : Async<'a * 'b> = async {
                let! a = a |> Async.StartChild
                let! b = b |> Async.StartChild
                let! a = a
                let! b = b
                return a, b }

        static member Parallel3(a : Async<'a>, b : Async<'b>, c : Async<'c>) : Async<'a * 'b * 'c> = async {
                let! a = a |> Async.StartChild
                let! b = b |> Async.StartChild
                let! c = c |> Async.StartChild
                let! a = a
                let! b = b
                let! c = c
                return a, b, c }

        static member ParallelWithThrottle (millisecondsTimeout : int) (limit : int) (items : 'a seq)
                        (operation : 'a -> Async<'b>) =
            let semaphore = new SemaphoreSlim(limit, limit)
            let mutable count = (items |> Seq.length)
            items
            |> Seq.map (fun item ->
                    async {
                        let! isHandleAquired = Async.AwaitTask
                                                <| semaphore.WaitAsync(millisecondsTimeout = millisecondsTimeout)
                        if isHandleAquired then
                            try
                                return! operation item
                            finally
                                if Interlocked.Decrement(&count) = 0 then semaphore.Dispose()
                                else semaphore.Release() |> ignore
                        else return! failwith "Failed to acquire handle"
                    })
            |> Async.Parallel

        /// Starts the specified operation using a new CancellationToken and returns
        /// IDisposable object that cancels the computation.
        static member StartCancelableDisposable(computation:Async<unit>) =
            let cts = new System.Threading.CancellationTokenSource()
            Async.Start(computation, cts.Token)
            { new IDisposable with member x.Dispose() = cts.Cancel() }

        static member StartContinuation (cont: 'a -> unit) (computation:Async<'a>) =
            Async.StartWithContinuations(computation,
                (fun res-> cont(res)),
                (ignore),
                (ignore))

        static member Map (map:'a -> 'b) (x:Async<'a>) = async {let! r = x in return map r}

        static member Tap (action:'a -> 'b) (x:Async<'a>) = (Async.Map action x) |> Async.Ignore|> Async.Start; x

module rec AsyncOperators =

    // ( <*> ) : f:Async<('a -> 'b)> -> m:Async<'a> -> Async<'b>
    let (<*>) = AsyncEx.apply
    // <!> : f:('a -> 'b) -> m:Async<'a> -> Async<'b>
    let (<!>) = AsyncEx.map

    let (<^>) = AsyncEx.``pure``

    // Bind
    // operation:('a -> Async<'b>) -> value:Async<'a> -> Async<'b>
    let inline (>>=) (operation:('a -> Async<'b>)) (value:Async<'a>) = async.Bind(value, operation)

    // Kliesli
    // val ( >=> ) : fAsync:('a -> Async<'b>) -> gAsync:('b -> Async<'c>) -> arg:'a -> Async<'c>
    let (>=>) (fAsync:'a -> Async<'b>) (gAsync:'b -> Async<'c>) (arg:'a) = async {
        let! f = Async.StartChild (fAsync arg)
        let! result = f
        return! gAsync result }

    [<RequireQualifiedAccess>]
    module AsyncEx =

        // x:'a -> Async<'a>
        let retn x = async.Return x

        // f:('b -> Async<'c>) -> a:Async<'b> -> Async<'c>
        let bind (f:'b -> Async<'c>) (a:Async<'b>) : Async<'c> = async.Bind(a, f)

        // map:('a -> 'b) -> value:Async<'a> -> Async<'b>
        let fmap (map : 'a -> 'b) (value : Async<'a>) : Async<'b> = async.Bind(value, map >> async.Return)

        let join (value:Async<Async<'a>>) : Async<'a> = async.Bind(value, id)

        //Listing 10.22 F# async applicative functor
        let ``pure`` (value:'a) = async.Return value  // #A

        // funAsync:Async<('a -> 'b)> -> opAsync:Async<'a> -> Async<'b>
        let apply (funAsync:Async<'a -> 'b>) (opAsync:Async<'a>) = async {
            // We start both async task in Parallel
            let! funAsyncChild = Async.StartChild funAsync // #B
            let! opAsyncChild = Async.StartChild opAsync

            let! funAsyncRes = funAsyncChild
            let! opAsyncRes = opAsyncChild  // #C
            return funAsyncRes opAsyncRes
            }

        let map (map : 'a -> 'b) (value : Async<'a>) : Async<'b> = async.Bind(value, map >> async.Return)

        let lift2 (func:'a -> 'b -> 'c) (asyncA:Async<'a>) (asyncB:Async<'b>) =
            func <!> asyncA <*> asyncB

        let lift3 (func:'a -> 'b -> 'c -> 'd) (asyncA:Async<'a>) (asyncB:Async<'b>) (asyncC:Async<'c>) =
            func <!> asyncA <*> asyncB <*> asyncC

        let tee (fn:'a -> 'b) (x:Async<'a>) = (map fn x) |> Async.Ignore|> Async.Start; x

        //Listing 10.27  Async-workflow conditional combinators
        let ifAsync (predicate:Async<bool>) funcA funcB =
            async.Bind(predicate, fun p -> if p then funcA else funcB)

        let notAsync predicate = async.Bind(predicate, not >> async.Return)

        let iffAsync (predicate:Async<'a -> bool>) (context:Async<'a>) = async {
            let! p = predicate <*> context
            return if p then Some context else None }

        let AND funcA funcB = ifAsync funcA funcB (async.Return false)
        let OR funcA funcB = ifAsync funcA (async.Return true) funcB
        let (<&&>) funcA funcB = AND funcA funcB
        let (<||>) funcA funcB = OR funcA funcB

        let traverse f list =
            let folder x xs = retn (fun x xs -> x :: xs) <*> f x <*> xs
            List.foldBack folder list (retn [])

        let sequence seq =
            let inline cons a b = lift2 (fun x xs -> x :: xs)  a b
            List.foldBack cons seq (retn [])

        // f:('a -> Async<'b>) -> x:'a list -> Async<'b list>
        let mapM f x = sequence (List.map f x)

        // xsm:Async<#seq<'b>> * f:('b -> 'c) -> Async<seq<'c>>
        let asyncFor(operations: #seq<'a> Async, f:'a -> 'b) =
            map (Seq.map map) operations

        // x2yR:('a -> Async<'b>) -> y2zR:('b -> Async<'c>) -> ('a -> Async<'c>)
        let andCompose x2yR y2zR = x2yR >=> y2zR

[<AutoOpen>]
module AsyncBuilderEx =

    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t : Task<'T>, f : 'T -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)
        member x.Bind(t : Task, f : unit -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task) = x.ReturnFrom(Async.AwaitTask computation)

        member this.Using(disp:#System.IDisposable, (f:Task<'T> -> Async<'R>)) : Async<'R> =
            this.TryFinally(f disp, fun () ->
                match disp with
                    | null -> ()
                    | disp -> disp.Dispose())

module AsyncHandler =

    type AsyncResult<'a> =
    | OK of 'a
    | Failure of exn
        static member ofChoice value =
            match value with
            | Choice1Of2 value -> AsyncResult.OK value
            | Choice2Of2 e -> Failure e

        static member ofOption optValue =
            match optValue with
            | Some value -> OK value
            | None -> Failure (ArgumentException())

    let handler operation = async {
        let! result = Async.Catch operation
        return
            match result with   // #B
            | Choice1Of2 result -> OK result
            | Choice2Of2 error  -> Failure error    }


    //  Listing 9.16 Implementation of mapHanlder Async-Combinator
    let mapHandler (continuation:'a -> Async<'b>)  comp = async {
        //Evaluate the outcome of the first future
        let! result = comp  // #A
        // Apply the mapping on success
        match result with
        | OK r -> return! handler (continuation r)    // #B
        | Failure e -> return Failure e     // #C
    }

    let map f =
        let map value = handler (async { return (f value) }) in mapHandler map

    let wrap (computation:Async<'a>) =
        async {
            let! choice = (Async.Catch computation)
            return (AsyncResult<'a>.ofChoice choice)
        }

    let wrapOptionAsync (computation:Async<'a option>) =
        async {
            let! choice = computation
            return (AsyncResult<'a>.ofOption choice)
        }
