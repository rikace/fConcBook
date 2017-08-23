module FSharpOptionExtensions

open System
open System.Runtime.CompilerServices
open FunInterop

/// Provides helpers for working with `FSharpOption<'T>` from languages other than F#
[<Sealed; Extension>]
type FSharpOptionExtensions =
    /// For `Some value`, applies the value to `withSome`; Otherwise, invokes `withNone`
    [<Extension>]
    static member Match(option, withSome :Action<'T>, withNone :Action) =
        match option with
        | Some value  -> (Fun.Of withSome) value
        | None        -> (Fun.Of withNone) ()

    /// For `Some value`, applies the value to `withSome`; Otherwise, invokes `withNone`
    [<Extension>]
    static member Match(option, withSome :Func<'T,'R>, withNone :Func<'R>) =
        match option with
        | Some value  -> (Fun.Of withSome) value
        | None        -> (Fun.Of withNone) ()

    /// For `Some value`, returns the value, otherwise returns the value of `getDefault`
    [<Extension>]
    static member GetValueOrDefault(option, getDefault :Func<'R>) =
        match option with
        | Some value -> value
        | None -> (Fun.Of getDefault) ()

    /// For `Some value`, returns the value, otherwise returns `defaultValue`
    [<Extension>]
    static member GetValueOrDefault(option, defaultValue) =
        match option with
        | Some value -> value
        | None -> defaultValue

    /// For `Some value`, returns the value, otherwise returns `default(T)`
    [<Extension>]
    static member GetValueOrDefault(option :Option<'T>) =
        match option with
        | Some value -> value
        | None -> Unchecked.defaultof<'T>

    /// Given a value, returns `Some value` if the value is not null, otherwise returns `None`
    [<Extension>]
    static member ToOption(value) =
        match value with |null -> None |value -> Some value

    /// Given a `Nullable<'T>`, returns `Some value` if `Nullable<'T>` has a value, otherwise returns `None`
    [<Extension>]
    static member ToOption(nullable :Nullable<'T>) =
        Option.ofNullable nullable