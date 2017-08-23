namespace Utilities.Interop

open System
open System.IO
open System.Net
open System.Collections.Generic
open System.Runtime.CompilerServices
open Microsoft.FSharp.Control.WebExtensions

[<Extension>]
type FSharpFunc =
    /// Convert an Action into an F# function returning unit
    static member FromAction (f: Action) =
        fun () -> f.Invoke()
    
    /// Convert an Action into an F# function returning unit
    static member FromAction (f: Action<_>) =
        fun x -> f.Invoke x

    /// Convert an Action into an F# function returning unit
    static member FromAction (f: Action<_,_>) =
        fun x y -> f.Invoke(x,y)

    /// Convert an Action into an F# function returning unit
    static member FromAction (f: Action<_,_,_>) =
        fun x y z -> f.Invoke(x,y,z)

    /// Convert a Func into an F# function
    static member FromFunc (f: Func<_>) =
        fun () -> f.Invoke()

    /// Convert a Func into an F# function
    static member FromFunc (f: Func<_,_>) =
        fun x -> f.Invoke x

    /// Convert a Func into an F# function
    static member FromFunc (f: Func<_,_,_>) =
        fun x y -> f.Invoke(x,y)

/// Extensions around Actions and Funcs
[<Extension>]
type Funcs =
    /// Converts an action to a function returning Unit
    [<Extension>]
    static member ToFunc (a: Action) =
        Func<_>(a.Invoke)

    /// Converts an action to a function returning Unit
    [<Extension>]
    static member ToFunc (a: Action<_>) =
        Func<_,_>(a.Invoke)
  
    /// Converts an action to a function returning Unit
    [<Extension>]
    static member ToFunc (f: Action<_,_>) =
        Func<_,_,_>(fun a b -> f.Invoke(a,b))

    /// Converts an action to a function returning Unit
    [<Extension>]
    static member ToFunc (f: Action<_,_,_>) =
        Func<_,_,_,_>(fun a b c -> f.Invoke(a,b,c))

    /// Converts an uncurried function to a curried function
    [<Extension>]
    static member Curry (f: Func<_,_,_>) =
        Func<_,Func<_,_>>(fun a -> Func<_,_>(fun b -> f.Invoke(a,b)))

    /// Converts an uncurried function to a curried function
    [<Extension>]
    static member Curry (f: Func<_,_,_,_>) =
        Func<_,Func<_,Func<_,_>>>(fun a -> Func<_,Func<_,_>>(fun b -> Func<_,_>(fun c -> f.Invoke(a,b,c))))

    /// Converts an action with 2 arguments into an action taking a 2-tuple
    [<Extension>]
    static member Tuple (f: Action<_,_>) =
        Action<_>(fun (a,b) -> f.Invoke(a,b))

    /// Converts an action with 3 arguments into an action taking a 3-tuple
    [<Extension>]
    static member Tuple (f: Action<_,_,_>) =
        Action<_>(fun (a,b,c) -> f.Invoke(a,b,c))

    /// Converts an action with 4 arguments into an action taking a 4-tuple
    [<Extension>]
    static member Tuple (f: Action<_,_,_,_>) =
        Action<_>(fun (a,b,c,d) -> f.Invoke(a,b,c,d))

    /// Converts an action taking a 2-tuple into an action with 2 parameters
    [<Extension>]
    static member Untuple (f: Action<_ * _>) =
        Action<_,_>(fun a b -> f.Invoke(a,b))

    /// /// Converts an action taking a 3-tuple into an action with 3 parameters
    [<Extension>]
    static member Untuple (f: Action<_ * _ * _>) =
        Action<_,_,_>(fun a b c -> f.Invoke(a,b,c))

    /// Converts an action taking a 4-tuple into an action with 4 parameters
    [<Extension>]
    static member Untuple (f: Action<_ * _ * _ * _>) =
        Action<_,_,_,_>(fun a b c d -> f.Invoke(a,b,c,d))

    /// Composes two functions.
    /// Mathematically: f . g
    [<Extension>]
    static member Compose (f: Func<_,_>, g: Func<_,_>) =
        Func<_,_>(fun x -> f.Invoke(g.Invoke(x)))

    /// Composes two functions (forward composition).
    /// Mathematically: g . f
    [<Extension>]
    static member AndThen (f: Func<_,_>, g: Func<_,_>) =
        Func<_,_>(fun x -> g.Invoke(f.Invoke(x)))
        