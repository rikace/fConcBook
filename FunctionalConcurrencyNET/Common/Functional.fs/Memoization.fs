namespace FunctionalConcurrency

[<AutoOpen>]
module Memoization =

    open System.Collections.Concurrent

    let memoize (f : 'a -> 'b) =
        let cache = new ConcurrentDictionary<'a,'b>()
        fun x -> cache.GetOrAdd(x, f)

    let memoize2 (f : 'a -> 'b -> 'c) =
        let f = (fun (a,b) -> f a b) |> memoize
        fun a b -> f (a,b)

    let memoize3 (f : 'a -> 'b -> 'c -> 'd) =
        let f = (fun (a,b,c) -> f a b c) |> memoize
        fun a b c -> f (a,b,c)
