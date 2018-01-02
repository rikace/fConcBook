module Kleisli
    open AsyncEx

    let kleisli (f:'a -> Async<'b>) (g:'b -> Async<'c>) (x:'a) = (f x) >>= g

    let (>=>) (operation1:'a -> Async<'b>) (operation2:'b -> Async<'c>) (value:'a) =
                                                                        operation1 value >>= operation2


