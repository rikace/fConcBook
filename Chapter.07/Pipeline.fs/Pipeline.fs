namespace Pipeline

open System
open System.Linq
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

// Listing 7.11  The IPipeline interface
[<Interface>]
type IPipeline<'a,'b> = // #A
    abstract member Then : ('b -> 'c) -> IPipeline<'a,'c>   // #B
    abstract member Enqueue : 'a * (('a * 'b) -> unit) -> unit  // #C
    abstract member Execute : int * CancellationToken -> IDisposable  // #D
    abstract member Stop : unit -> unit // #E


// Listing 7.12 The Parallel Functional Pipeline pattern
[<Struct>]
type internal Continuation<'a, 'b>(input:'a, callback:('a * 'b) -> unit) =
    member this.Input with get() = input
    member this.Callback with get() = callback  // #A

type Pipeline<'a, 'b> private (func:'a -> 'b) as this =
    let continuations = Array.init 3 (fun _ -> new BlockingCollection<Continuation<'a,'b>>(100))    // #B

    let then' (nextFunction:'b -> 'c) =
        Pipeline(func >> nextFunction) :> IPipeline<_,_>    // #C

    let enqueue (input:'a) (callback:('a * 'b) -> unit) =
        BlockingCollection<Continuation<_,_>>.AddToAny(continuations, Continuation(input, callback))    // #D

    let stop() = for continuation in continuations do continuation.CompleteAdding() // #E

    let execute blockingCollectionPoolSize (cancellationToken:CancellationToken) =

        cancellationToken.Register(Action(stop)) |> ignore  // #F

        for i = 0 to blockingCollectionPoolSize - 1 do
            Task.Factory.StartNew(fun ( )-> // #G
                while (not <| continuations.All(fun bc -> bc.IsCompleted)) && (not <| cancellationToken.IsCancellationRequested) do
                    let continuation = ref Unchecked.defaultof<Continuation<_,_>>
                    BlockingCollection.TakeFromAny(continuations, continuation)
                    let continuation = continuation.Value
                    continuation.Callback(continuation.Input, func(continuation.Input))

            ,cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default) |> ignore

    static member Create(func:'a -> 'b) =
        Pipeline(func) :> IPipeline<_,_> // #H

    interface IPipeline<'a, 'b> with
        member this.Then(nextFunction) = then' nextFunction
        member this.Enqueue(input, callback) = enqueue input callback |> ignore
        member this.Stop() = stop()
        member this.Execute (blockingCollectionPoolSize, cancellationToken) =
            execute blockingCollectionPoolSize cancellationToken
            { new IDisposable with member self.Dispose() = stop() }
