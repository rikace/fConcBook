namespace StockTicker

open System
open StockTicker.Events
open StockTicker.Core
open EventStorage
open StockMarket
open Events


type SendMessageWith<'a> =
    | SendMessageWith of string * 'a


//Computation expressions in F# provide a convenient syntax for writing
//computations that can be sequenced and combined using control flow constructs
//and bindings. They can be used to provide a convenient syntax for monads,
//a functional programming feature that can be used to manage data, control,
//and side effects in functional programs.
//Computation expressions in F# provide a convenient syntax for writing computations
//that can be sequenced and combined using control flow constructs and bindings.

[<AutoOpenAttribute>]
module RetryPublishMonad =
    open System.Threading.Tasks

    // Listing 9.4 Retry Async Builder
    type RetryAsyncBuilder(max, sleepMilliseconds : int) =
        let rec retry n (task:Async<'a>) (continuation:'a -> Async<'b>) = async {
            try
                let! result = task  // #A
                let! conResult = continuation result // #B
                return conResult
            with error ->
                if n = 0 then return raise error // #C
                else
                    do! Async.Sleep sleepMilliseconds // #D
                    return! retry (n - 1) task continuation }

        member this.ReturnFrom(f) = f // #E
        member this.Return(v) = async { return v } // #F
        member this.Delay(f) = async { return! f() } // #G
        member this.Bind(task:Async<'a>, continuation:'a -> Async<'b>) = retry max task continuation // #H
        member this.Bind(task : Task<'T>, continuation : 'T -> Async<'R>) : Async<'R> = this.Bind(Async.AwaitTask task, continuation)
        member this.Zero() = this.Return()
