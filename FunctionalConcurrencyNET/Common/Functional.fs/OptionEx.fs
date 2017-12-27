namespace FunctionalConcurrency

type AsyncOption<'a> = Async<Option<'a>>

[<RequireQualifiedAccess>]
module Option =

    let ofChoice choice =
        match choice with
        | Choice1Of2 value -> Some value
        | Choice2Of2 _ -> None

[<RequireQualifiedAccess>]
module AsyncOption =
    let handler (operation:Async<'a>) : AsyncOption<'a> = async {
        let! result = Async.Catch operation
        return (Option.ofChoice result)
    }

[<AutoOpen>]
module OptionEx =

    let apply f a =
        match f with
        | Some f -> Option.map f a
        | None -> None

    //let apply' fOpt xOpt =
    //    fOpt |> Option.bind (fun f ->
    //        let map = Option.bind (f >> Some)
    //        map xOpt)

    let ``pure`` f = Some f

    let (<*>) = apply
    let (>>=) a f = Option.bind(f) |> a
    let (<!>) f x = Option.map f x

    //printfn "%A" ((+) <!> Some 5 <*> Some 4)    // Quick tests
    //printfn "%A" ((+) <!> Some 5 <*> None  )
    //let mul a b = a * b
    //let m = mul <!> (Some 3) <*> (Some 8)
    let joinOption opt =
        match opt with
        | None          -> None
        | Some innerOpt -> innerOpt

    // f:('a -> 'b -> 'c) -> x:'a option -> y:'b option -> 'c option
    let lift2 f x y     = f <!> x <*> y
    //  f:('a -> 'b -> 'c -> 'd) ->  x:'a option -> y:'b option -> z:'c option -> 'd option
    let lift3 f x y z   = f <!> x <*> y <*> z
    let lift4 f x y z w = f <!> x <*> y <*> z <*> w

    let filter f opt =
        match opt with
        | None -> None
        | Some x -> if f x then opt else None

    let ofNull<'T when 'T : not struct> (t : 'T) =
        if obj.ReferenceEquals(t, null) then None
        else
            Some t


    module Bind_vs_Apply_vs_Map =

        // The combination of bind and return are considered even more powerful than apply and return,
        // because if you have bind and return, you can construct map and apply from them, but not vice versa.

        // map defined in terms of bind and return (Some)
        let map f = Option.bind (f >> ``pure``)

        let (<!>) f a = ``pure`` f <*> a

        // apply defined in terms of bind and return (Some)
        // ('a -> 'b option) -> 'a option -> 'b option
        let (>>=) x f = Option.bind f x

        // How to compose world-crossing functions
        // bind, flatMap, SelectMany
        let (>=>) f a = f >> (Option.bind a)