namespace FunctionalConcurrency

[<AutoOpen>]
module Utilities =

    let inline flip f a b = f b a 

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    let inline tap fn x = fn x |> ignore; x
