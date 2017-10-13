namespace FunctionalConcurrency

open System.Threading.Tasks

type AsyncResult<'a> = Async<Result<'a>>

module AsyncResult =
    open System.Threading.Tasks
    open System.Threading

    let handler (operation:Async<'a>) : AsyncResult<'a> = async {
        let! result = Async.Catch operation     // #B
        return (Result.ofChoice result) }       // #C

    //Listing 10.13 Higher order function extending the AsyncResult type
    let retn (value:'a) : AsyncResult<'a> =  value |> Ok |> async.Return // #A

    let map (selector : 'a -> Async<'b>) (asyncResult : AsyncResult<'a>) : AsyncResult<'b> = async {
        let! result = asyncResult
        match result with
        | Ok x -> return! selector x |> handler  // #B
        | Error err -> return (Error err)   }   // #C

    let bind (selector : 'a -> AsyncResult<'b>) (asyncResult : AsyncResult<'a>) = async {
        let! result = asyncResult
        match result with
        | Ok x -> return! selector x    // #B
        | Error err -> return Error err    }    // #D

    let bimap success failure operation = async {
        let! result = operation
        match result with
        | Ok v -> return! success v |> handler   // #B
        | Error x -> return! failure x |> handler }        // #E





        // TODO
    let mapChoice (f:'a -> Result<'b>) (a:AsyncResult<'a>) : AsyncResult<'b> =
        a |> Async.map (function
            | Ok a' -> f a'
            | Error e -> Error e)

    let bindChoice (f:'a -> AsyncResult<'b>) (a:AsyncResult<'a>) : AsyncResult<'b> =
            a |> Async.bind (function
              | Ok a' -> f a'
              | Error e ->  Error e |> async.Return)

    // computations:seq<Async<'a>> -> Async<Result<'a,exn> []>
    let parallelCatch computations  =
        computations
        |> Seq.map Async.Catch
        |> Seq.map (Async.map Result.ofChoice)
        |> Async.Parallel





    let apply (ap : AsyncResult<'a -> 'b>) (asyncResult : AsyncResult<'a>) : AsyncResult<'b> = async {
        let! result = asyncResult |> Async.StartChild
        let! fap = ap |> Async.StartChild
        let! fapResult = fap
        let! fResult = result
        match fapResult, fResult with
        | Ok ap, Ok result -> return ap result |> Ok
        | Error err, _
        | _, Error err -> return Error err    }


    let defaultValue value =
        Async.map (Result.defaultValue value)

    // TODO             
    let inline EITHER (funcA:AsyncResult<'a>) (funcB:AsyncResult<'a>) : AsyncResult<'a> =
        let tcs = TaskCompletionSource()
        let reportResult =
            let counter = ref 0
            (fun (func:AsyncResult<'a>) ->
                async {
                    let! result = func
                    match result with
                    | Ok (x) -> tcs.TrySetResult(Ok x) |> ignore
                    | Error(e) ->
                        if !counter = 0
                        then Interlocked.Increment(counter) |> ignore
                        else tcs.SetResult(Error e)
                })

        [funcA; funcB]
        |> List.map reportResult
        |> Async.Parallel
        |> Async.StartChild
        |> ignore

        Async.AwaitTask tcs.Task

type AsyncResultBuilder()=
    member this.Return m = AsyncResult.retn m
    member this.Bind (m, f:'a -> AsyncResult<'b>) = AsyncResult.bind f m
    member this.Bind (m:Task<'a>, f:'a -> AsyncResult<'b>) = AsyncResult.bind f (m |> Async.AwaitTask |> AsyncResult.handler)
    member this.Bind (m:Task, f) = AsyncResult.bind f (m |> Async.AwaitTask |> AsyncResult.handler)
    member this.ReturnFrom m = m
    member this.Combine (funcA:AsyncResult<'a>, funcB:AsyncResult<'a>) = async {
        let! a = funcA
        match a with
        | Ok _ -> return a
        | _ -> return! funcB }

    member this.Zero() = this.Return()
    member this.Delay(f : unit -> AsyncResult<'a>) : AsyncResult<'a> = async.Delay(f)
    member this.Yield(x) = x |> async.Return |> AsyncResult.handler
    member this.YieldFrom(m) = m

    member this.Using(resource : 'T when 'T :> System.IDisposable, binder : 'T -> AsyncResult<'a>) : AsyncResult<'a> =
        async.Using(resource, binder)

[<AutoOpen>]
module AsyncResultBuilder =
    let asyncResult = AsyncResultBuilder()

    let (<!>) = AsyncResult.map
    let (<*>) = AsyncResult.apply

[<RequireQualifiedAccess>]
module AsyncComb =

    // x:'a -> Async<'a>
    let inline retn x = async.Return x

    // f:('b -> Async<'c>) -> a:Async<'b> -> Async<'c>
    let inline bind (f:'b -> Async<'c>) (a:Async<'b>) : Async<'c> = async.Bind(a, f)

    // af:Async<('a -> 'b)> -> ax:Async<'a> -> Async<'b>
    let inline apply af ax = async {
        let! pf = Async.StartChild af
        let! px = Async.StartChild ax
        let! f = pf
        let! x = px
        return f x
    }

    // f:('a -> 'b) -> m:Async<'a> -> Async<'b>
    let inline map f m = async.Bind(m, f >> async.Return)

    // (Async<('a -> 'b)> -> Async<'a> -> Async<'b>)
    let (<*>)  = apply
    // (('a -> 'b) -> Async<'a> -> Async<'b>)
    let (<!>) = map

    // f:('a -> Async<'b>) -> list:'a list -> Async<'b list>
    let inline traverse f list =
        let folder x xs = retn (fun x xs -> x :: xs) <*> f x <*> xs
        List.foldBack folder list (retn [])

    // f:('a -> 'b -> 'c) -> a:Async<'a> -> b:Async<'b> -> Async<'c>
    let inline lift2 f a b = f <!> a <*> b
    // f:('a -> 'b -> 'c -> 'd) -> A:Async<'a> -> b:Async<'b> -> c:Async<'c> -> Async<'d>
    let inline lift3 f a b c = f <!> a <*> b <*> c

    // predicate:Async<bool> -> funcA:Async<'a> -> funcB:Async<'a> -> Async<'a>
    let inline ifAsync predicate funcA funcB =
        async.Bind(predicate, fun p -> if p then funcA else funcB)

    // (Context -> bool) -> Context -> Async<Context option>
    let iff predicate funcA funcB value =
        async.Bind(predicate value, fun p -> if p then funcA value else funcB value)

    let iffAsync (predicate:Async<'a -> bool>) (context:Async<'a>) = async {
        let! p = predicate <*> context
        return if p then Some context else None }

    // predicate:Async<bool> -> Async<bool>
    let inline notAsync (predicate:Async<bool>) = async.Bind(predicate, not >> async.Return)

    //// funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let inline AND (funcA:Async<bool>) (funcB:Async<bool>) = ifAsync funcA funcB (async.Return false)

    // funcA:Async<bool> -> funcB:Async<bool> -> Async<bool>
    let inline OR (funcA:Async<bool>) (funcB:Async<bool>) = ifAsync funcA (async.Return true) funcB

[<AutoOpen>]
module AsyncResultCombinators =

    let inline AND (funcA:AsyncResult<'a>) (funcB:AsyncResult<'a>) : AsyncResult<_> =
        asyncResult {
                let! a = funcA
                let! b = funcB
                return (a, b)
        }
    let inline OR (funcA:AsyncResult<'a>) (funcB:AsyncResult<'a>) : AsyncResult<'a> =
        asyncResult {
            return! funcA
            return! funcB
        }

    // funcA:AsyncResult<'a> -> funcB:AsyncResult<'a> -> AsyncResult<'a * 'a>
    let (<&&>) (funcA:AsyncResult<'a>) (funcB:AsyncResult<'a>) = AND funcA funcB
    // funcA:AsyncResult<'a> -> funcB:AsyncResult<'a> -> AsyncResult<'a>
    let (<||>) (funcA:AsyncResult<'a>) (funcB:AsyncResult<'a>) = OR funcA funcB

    let gt value (ar:AsyncResult<'a>) =
        asyncResult {
            let! result = ar
            return result > value
        }

    let (<|||>) (funcA:AsyncResult<bool>) (funcB:AsyncResult<bool>) =
        asyncResult {
            let! rA = funcA
            match rA with
            | true -> return! funcB
            | false -> return false
        }

    let (<&&&>) (funcA:AsyncResult<bool>) (funcB:AsyncResult<bool>) =
        asyncResult {
            let! (rA, rB) = funcA <&&> funcB
            return rA && rB
        }