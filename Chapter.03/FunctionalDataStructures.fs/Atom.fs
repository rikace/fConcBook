module Atom

open System.Threading

let eq a b = obj.ReferenceEquals(a,b)
let neq a b = eq a b |> not

type Atom<'T when 'T : not struct>(value : 'T) =
    let cell = ref value
    let spinner = lazy (new SpinWait())

    let rec swap f =
        let tempValue = !cell
        if Interlocked.CompareExchange<'T>(cell, f tempValue, tempValue) |> neq tempValue then
            spinner.Value.SpinOnce()
            swap f


    member x.Value with get() = !cell
    member x.Swap (f : 'T -> 'T) = swap f

[<RequireQualifiedAccess>]
module Atom =
    let atom value = new Atom<_>(value)

    let swap (atom : Atom<_>) (f : _ -> _) = atom.Swap f