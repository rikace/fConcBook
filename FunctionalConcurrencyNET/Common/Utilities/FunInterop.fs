module FunInterop

open System.Runtime.CompilerServices
open System

[<Extension>]
type public FSharpFuncUtil =

    [<Extension>]
    static member ToFSharpFunc<'a,'b> (func:System.Converter<'a,'b>) = fun x -> func.Invoke(x)

    [<Extension>]
    static member ToFSharpFunc<'a,'b> (func:System.Func<'a,'b>) = fun x -> func.Invoke(x)

    [<Extension>]
    static member ToFSharpFunc<'a,'b,'c> (func:System.Func<'a,'b,'c>) = fun x y -> func.Invoke(x,y)

    [<Extension>]
    static member ToFSharpFunc<'a,'b,'c,'d> (func:System.Func<'a,'b,'c,'d>) = fun x y z -> func.Invoke(x,y,z)

    [<Extension>]
    static member ToFSharpAction<'a,'b> (func:System.Action<'a,'b>) = fun x y  -> func.Invoke(x,y)



    static member Create<'a,'b> (func:System.Func<'a,'b>) = FSharpFuncUtil.ToFSharpFunc func

    static member Create<'a,'b,'c> (func:System.Func<'a,'b,'c>) = FSharpFuncUtil.ToFSharpFunc func

    static member Create<'a,'b,'c,'d> (func:System.Func<'a,'b,'c,'d>) = FSharpFuncUtil.ToFSharpFunc func



/// Provides helpers for converting CLR delegates (`Func` and `Action`) into F# functions
[<Extension>]
type Fun =
    /// Converts `Func` of no arguments into an F# function
    [<Extension>]
    static member Of(fn :Func<'R>) = (fun () -> fn.Invoke())
    /// Converts `Func` of 1 argument into an F# function
    [<Extension>]
    static member Of(fn :Func<'T,'R>) = (fun a -> fn.Invoke(a))
    /// Converts `Func` of 2 arguments into an F# function
    [<Extension>]
    static member Of(fn :Func<'T,'U,'R>) = (fun a b -> fn.Invoke(a,b))
    /// Converts `Func` of 3 arguments into an F# function
    [<Extension>]
    static member Of(fn :Func<'T,'U,'V,'R>) = (fun a b c -> fn.Invoke(a,b,c))
    /// Converts `Action` of no arguments into an F# function
    [<Extension>]
    static member Of(act :Action) = (fun () -> act.Invoke())
    /// Converts `Action` of 1 argument into an F# function
    [<Extension>]
    static member Of(act :Action<'T>) = (fun a -> act.Invoke(a))
    /// Converts `Action` of 2 arguments into an F# function
    [<Extension>]
    static member Of(act :Action<'T,'U>) = (fun a b -> act.Invoke(a,b))
    /// Converts `Action` of 3 arguments into an F# function
    [<Extension>]
    static member Of(act :Action<'T,'U,'V>) = (fun a b c -> act.Invoke(a,b,c))



//
//[<Sealed; Extension; CompiledName("Result")>]
//type ResultExtensions =
//    static member TryCatch(attempt :Func<_>) =
//        Result.tryCatch (Fun.Of attempt)
//
//    static member Divide(results) =
//        let (os,xs) = Result.divide results
//        (ResizeArray os,ResizeArray xs)
//
//    [<Extension>]
//    static member ToOK<'T,'Error>(value :'T) :Result<'T,'Error> = Result.ok value
//
//    [<Extension>]
//    static member ToError<'T,'Error>(value :'Error) :Result<'T,'Error> = Result.error value
//
//    [<Extension>]
//    static member Match(result, withOK :Action<_>, withError :Action<_>) =
//        result |> Result.bimap (Fun.Of withOK) (Fun.Of withError)
//
//    [<Extension>]
//    static member Match(result, withOK :Func<_,_>, withError :Func<_,_>) =
//        result |> Result.bimap (Fun.Of withOK) (Fun.Of withError)
//
//    [<Extension>]
//    static member IfOK(result, withOK :Action<'a>) =
//        result |> Result.iter (Fun.Of withOK)
//
//    [<Extension>]
//    static member IfError(result, withError :Action<_>) =
//        result |> Result.iterError (Fun.Of withError)
//
//    [<Extension>]
//    static member IsOK(result) = Result.isOK result
//
//    [<Extension>]
//    static member IsError(result) = Result.isError result
//
//    [<Extension>]
//    static member GetOrRaise(result, withError) =
//        result |> Result.getOrRaise (Fun.Of<_,_> withError)
//
//    [<Extension>]
//    static member Select(result, select) =
//        result |> Result.map (Fun.Of<_,_> select)
//
//    [<Extension>]
//    static member SelectMany(result, select) =
//        result |> Result.bind (Fun.Of<_,_> select)
//
//    [<Extension>]
//    static member SelectMany(result, select, merge) =
//        result |> Result.bind (fun v ->
//            let select = Fun.Of<_,_> select
//            let merge = Fun.Of<_,_,_> merge
//            v |> select |> Result.map (merge v))
//
//    [<Extension>]
//    static member Join  (outer
//                        ,inner
//                        ,_selectOuter :Func<'T,'K> // unused
//                        ,_selectInner :Func<'U,'K> // unused
//                        ,merge        :Func<'T,'U,'C>) =
//        match (outer,inner) with
//        | Ok v1,Ok v2 ->
//            let merge = Fun.Of merge
//            Result.ok (merge v1 v2)
//        | Err x,_
//        | _,Err x -> Result.error x

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