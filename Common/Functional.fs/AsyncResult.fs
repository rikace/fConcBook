namespace FunctionalConcurrency

open System.Threading.Tasks

type AsyncResult<'a> = Async<Result<'a>>

module AsyncResult =
    open AsyncOperators
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

    let mapChoice (f:'a -> Result<'b>) (a:AsyncResult<'a>) : AsyncResult<'b> =
        a |> AsyncEx.map (function
            | Ok a' -> f a'
            | Error e -> Error e)

    let bindChoice (f:'a -> AsyncResult<'b>) (a:AsyncResult<'a>) : AsyncResult<'b> =
            a |> AsyncEx.bind (function
              | Ok a' -> f a'
              | Error e ->  Error e |> async.Return)

    // computations:seq<Async<'a>> -> Async<Result<'a,exn> []>
    let parallelCatch computations  =
        computations
        |> Seq.map Async.Catch
        |> Seq.map (AsyncEx.map Result.ofChoice)
        |> Async.Parallel

    // The parallelCatch can be re-written using the AsyncHandler.handler as here
    //let parallelCatch computations  =
    //    computations
    //    |> Seq.map AsyncHandler.handler
    //    |> Async.Parallel

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
        AsyncEx.map (Result.defaultValue value)

type AsyncResultBuilder() =
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