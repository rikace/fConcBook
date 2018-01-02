module LazyList

// Listing 3.15 Lazy list implementation using F#
type LazyList<'a> =
    | Cons of head:'a * tail:Lazy<'a LazyList>  //#A
    | Empty
let empty = lazy(Empty)                         //#B

let rec append items list =                     //#C
    match items with
    | Cons(head, Lazy(tail)) ->
        Cons(head, lazy(append tail list))      //#D
    | Empty -> list

let list1 = Cons(42, lazy(Cons(21, empty)))     //#E
// val list1: LazyList<int> = Cons (42,Value is not created.)

let list = append (Cons(3, empty)) list1        //#F
// val list : LazyList<int> = Cons (3,Value is not created.)

let rec iter action list =                      //#G
    match list with
    | Cons(head, Lazy(tail)) ->
        action(head)
        iter action tail
    | Empty -> ()

list |> iter (printf "%d .. ")                  //#H
// 3 .. 42 .. 21 ..
