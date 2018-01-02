module AsyncEx

[<AutoOpen>]
module Async =

    let retn x = async { return x }

    let bind (operation:'a -> Async<'b>) (xAsync:Async<'a>) = async {
        let! x = xAsync
        return! operation x }

    let (>>=) (item:Async<'a>) (operation:'a -> Async<'b>) = bind operation item

    let run continuation op = Async.StartWithContinuations(op, continuation, (ignore), (ignore))
