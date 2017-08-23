module FList

// Listing 3.13 Representation of a list in F# using discriminated unions
type FList<'a> =
    | Empty                             //#A
    | Cons of head:'a * tail:FList<'a>  //#B

let rec map f (list:FList<'a>) =        //#C
    match list with
    | Empty -> Empty
    | Cons(hd,tl) -> Cons(f hd, map f tl)

let rec filter p (list:FList<'a>) =
    match list with
    | Empty -> Empty
    | Cons(hd,tl) when p hd = true -> Cons(hd, filter p tl)
    | Cons(hd,tl) -> filter p tl

let list = Cons(5, Cons(4, Cons(3, Empty)))
