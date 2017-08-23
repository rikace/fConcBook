module MemoizationEx

    open System.Collections.Concurrent

    let memoize (f : 'a -> 'b) =
        let cache = new ConcurrentDictionary<'a,'b>()
        fun x -> cache.GetOrAdd(x, f)

    let memoize2 f =
        let f = (fun (a,b) -> f a b) |> memoize
        fun a b -> f (a,b)

    let memoize3 f =
        let f = (fun (a,b,c) -> f a b c) |> memoize
        fun a b c -> f (a,b,c)


//    let add x y =
//        printfn "computing %d %d = %d" x y (x + y)
//        x + y
//
//    let addM = memoize2 add
//
//    addM 3 4
//    addM 5 6
//    addM 3 4