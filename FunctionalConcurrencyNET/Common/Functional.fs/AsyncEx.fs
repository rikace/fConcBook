namespace FunctionalConcurrency

open System
open System.IO
open System.Threading.Tasks
open System.Threading

[<AutoOpen>]
module AsyncEx =

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

        static member StartContinuations (cont: 'a -> unit) (computation:Async<'a>) =
            Async.StartWithContinuations(computation,
                (fun res-> cont(res)),
                (ignore),
                (ignore))

    type StreamReader with
        member this.AsyncReadToEnd() : Async<string> = async {
            use asyncReader = new AsyncStreamReader(this.BaseStream)
            return! asyncReader.ReadToEnd() }
            
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
        let f' value = handler (async { return (f value) }) in mapHandler f'

module AsyncOperators =

    type Outcome<'a> =
        | Success of 'a
        | Failure of Exception
        static member ofChoice value =
            match value with
            | Choice1Of2 value -> Success value
            | Choice2Of2 e -> Failure e
        static member ofOption optValue =
            match optValue with
            | Some value -> Success value
            | None -> Failure (ArgumentException())


    let wrap (computation:Async<'a>) =
        async {
            let! choice = (Async.Catch computation)
            return (Outcome<'a>.ofChoice choice)
        }

    let wrapOptionAsync (computation:Async<'a option>) =
        async {
            let! choice = computation
            return (Outcome<'a>.ofOption choice)
        }

    let retn x = async.Return x

    let apply af ax = async {
        // We start both async task in Parallel
        let! pf = Async.StartChild af
        let! px = Async.StartChild ax
        // We then wait that both async operations complete
        let! f = pf
        let! x = px
        // Finally we execute (f x)
        return f x
    }

    let (<*>)   = apply
    let map f x = retn f <*> x
    let (<!>) = map

    let traverse f list =
        let folder x xs = retn (fun x xs -> x :: xs) <*> f x <*> xs
        List.foldBack folder list (retn [])

    let ffmap f m = async.Bind(m, f >> async.Return)

    let bind (f:'b -> Async<'c>) (a:Async<'b>) : Async<'c> = async.Bind(a, f)

    let fmap f x = async {let! r = x in return f r}

    //let lift2 f a b =
    //    f <!> a <*> b

    //let lift3 f a b c =
    //    f <!> a <*> b <*> c

    let lift2 (func:'a -> 'b -> 'c) (asyncA:Async<'a>) (asyncB:Async<'b>) =
        func <!> asyncA <*> asyncB

    let lift3 (func:'a -> 'b -> 'c -> 'd) (asyncA:Async<'a>) (asyncB:Async<'b>) (asyncC:Async<'c>) =
        func <!> asyncA <*> asyncB <*> asyncC

    //Listing 10.27  Async-workflow conditional combinators
    let ifAsync (predicate:Async<bool>) funcA funcB =
        async.Bind(predicate, fun p -> if p then funcA else funcB)

    let iffAsync (predicate:Async<'a -> bool>) (context:Async<'a>) = async {
        let! p = predicate <*> context
        return if p then Some context else None }

    let AND funcA funcB = ifAsync funcA funcB (async.Return false)
    let OR funcA funcB = ifAsync funcA (async.Return true) funcB
    let (<&&>) funcA funcB = AND funcA funcB
    let (<||>) funcA funcB = OR funcA funcB



    //let ifAsync predicate funcA funcB = async.Bind(predicate, fun p -> if p then funcA else funcB)
    let notAsync predicate = async.Bind(predicate, not >> async.Return)
    //let AND funcA funcB = ifAsync funcA funcB (async.Return false)
    //let OR funcA funcB = ifAsync funcA (async.Return true) funcB
    //let (<&&>) funcA funcB = AND funcA funcB
    //let (<||>) funcA funcB = OR funcA funcB



[<RequireQualifiedAccess>]
module Async =
    // x:'a -> Async<'a>
    let inline retn x = async.Return x

    // f:('b -> Async<'c>) -> a:Async<'b> -> Async<'c>
    let inline bind (f:'b -> Async<'c>) (a:Async<'b>) : Async<'c> = async.Bind(a, f)

    // funAsync:Async<('a -> 'b)> -> opAsync:Async<'a> -> Async<'b>
    let apply funAsync opAsync = async {
        let! funAsyncChild = Async.StartChild funAsync // #B
        let! opAsyncChild = Async.StartChild opAsync

        let! funAsyncRes = funAsyncChild
        let! opAsyncRes = opAsyncChild  // #C
        return funAsyncRes opAsyncRes
    }

    // val ( >>= ) : f:Async<'a> -> a:('a -> Async<'b>) -> Async<'b>
    // f:('a -> 'b) -> m:Async<'a> -> Async<'b>
    let inline map (map : 'a -> 'b) (value : Async<'a>) : Async<'b> = async.Bind(value, map >> async.Return)

    // func:('a -> 'b) -> operation:Async<'a> -> Async<'b>
    //let inline fmap (func : 'a -> 'b) (operation : Async<'a>) : Async<'b> = async {
    //    let! result = operation
    //    return func result
    //}

    // <!> : f:('a -> 'b) -> m:Async<'a> -> Async<'b>
    let (<!>) f value = map f value

    // ( <*> ) : f:Async<('a -> 'b)> -> m:Async<'a> -> Async<'b>
    let (<*>) f m = apply f m

    //// 'a -> Async<'a>
    //let inline ``pure`` (value:'a) = async.Return (value:'a)
    // x:Async<Async<'T>> -> Async<'T>
    let inline join (value:Async<Async<'a>>) : Async<'a> = async.Bind(value, id)
    //// funAsync:Async<('a -> 'b)> -> opAsync:Async<'a> -> Async<'b>
    //let inline apply (funAsync:Async<('a -> 'b)>) (opAsync:Async<'a>) = async {
    //    let! funAsyncChild = Async.StartChild funAsync
    //    let! opAsyncChild  = Async.StartChild opAsync
    //    let! funAsyncRes = funAsyncChild
    //    let! opAsyncRes  = opAsyncChild
    //    return funAsyncRes opAsyncRes
    //}

    //Listing 10.22 F# async applicative functor
    let ``pure`` value = async.Return value  // #A

    // f:('a -> Async<'b>) -> list:'a list -> Async<'b list>
    let inline traverse f list =
        let folder x xs = retn (fun x xs -> x :: xs) <*> f x <*> xs
        List.foldBack folder list (retn [])

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    //let inline tee (fn:'a -> 'b) (x:Async<'a>) =
    //    async { let! result = x
    //            return fn result }
    //    |> Async.Ignore |> Async.Start; x
    //let inline tee (fn:Async<'a> -> 'b) (x:Async<'a>) =
    //    async { let! result = fn x
    //            return result }
    //    |> Async.Ignore |> Async.Start; x

    let inline tee (fn:'a -> 'b) (x:Async<'a>) = (map fn x) |> Async.Ignore|> Async.Start; x

    // f:('a -> 'b -> 'c) -> x:Async<'a> -> y:Async<'b> -> Async<'c>
    let inline lift2 f x y = retn f <*> x <*> y
    // f:('a -> 'b -> 'c -> 'd) -> x:Async<'a> -> y:Async<'b> -> z:Async<'c> -> Async<'d>
    let inline lift3 f x y z = retn f <*> x <*> y <*> z

    // Convert a list of Async into a Async<list> using applicative style.
    let inline listcons a b = a::b
    // s:Async<'a> list -> Async<'a list>
    let inline sequence s =
        let inline cons a b = lift2 listcons a b
        List.foldBack cons s (retn [])

    // f:('a -> Async<'b>) -> x:'a list -> Async<'b list>
    let inline mapM f x = sequence (List.map f x)

    // xsm:Async<#seq<'b>> * f:('b -> 'c) -> Async<seq<'c>>
    let inline asyncFor(operations: #seq<'a> Async, f:'a -> 'b) =
        map (Seq.map map) operations

        // predicate:Async<bool> -> funcA:Async<'a> -> funcB:Async<'a> -> Async<'a>
    let inline ifAsync predicate funcA funcB = async.Bind(predicate, fun p -> if p then funcA else funcB)

    //let inline ifAsync predicate item funcA funcB = async.Bind(predicate, fun p -> if p then funcA else funcB)
    //let ifAsync predicate funcA funcB =
    //    async.Bind(predicate, fun p -> if p then funcA else funcB)

      // (Context -> bool) -> Context -> Async<Context option>
    let iff predicate funcA funcB value = async.Bind(predicate value, fun p -> if p then funcA value else funcB value)
        //if condition value then
        //  value |> Ok |> async.Return
        //else
        //  Error "" |> async.Return


    // predicate:Async<bool> -> Async<bool>
    let inline notAsync predicate = async.Bind(predicate, not >> async.Return)
    // funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let inline AND funcA funcB = ifAsync funcA funcB (async.Return false)
    // funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let inline OR funcA funcB = ifAsync funcA (async.Return true) funcB
    // funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let (<&&>) funcA funcB = AND funcA funcB
    // funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let (<||>) funcA funcB = OR funcA funcB


module AsyncInfixOperators =

    // m:('a -> Async<'b>) -> f:Async<'a> -> Async<'b>
    let inline (>>=) operation value = async.Bind(value, operation)
    // Kliesli
    // val ( >=> ) : f1:('a -> Async<'b>) -> f2:('b -> Async<'c>) -> arg:'a -> Async<'c>
    //let (>=>) f1 f2 arg = f1 arg >>= f2
    let (>=>) fAsync gAsync arg = async {
        let! f = Async.StartChild (fAsync arg)
        let! result = f
        return! gAsync result }

    // x2yR:('a -> Async<'b>) -> y2zR:('b -> Async<'c>) -> ('a -> Async<'c>)
    let andCompose x2yR y2zR = x2yR >=> y2zR

    //Defining map and apply through bind
    //The combination of return and bind is really powerful. In Understanding apply we already saw that we can implement map through return and apply. But with return and bind we can easily implement map and apply.
    // map with bind operator
    module ConceptBind =
        let map f opt =
            async.Bind(opt, (fun x -> // unbox option
                async.Return (f x)    // execute (f x) and box result
            ))

    // Apply with bind operator
        let apply fo xo =
            async.Bind(fo, (fun f ->  // unbox function
                async.Bind(xo, (fun x ->  // unbox value
                    async.Return (f x)    // execute (f x) and box result
            ))))
