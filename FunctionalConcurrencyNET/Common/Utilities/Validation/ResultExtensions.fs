namespace Validation

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open ResultEx
open FunInterop


[<Sealed; Extension; CompiledName("Result")>]
type ResultExtensions =
    static member TryCatch(attempt :Func<_>) =
        ResultEx.tryCatch (Fun.Of attempt)

    [<Extension>]
    static member ToOK<'T,'Error>(value :'T) : Result<'T,'Error> = Result.Ok value

    [<Extension>]
    static member ToError<'T,'Error>(value :'Error) :Result<'T,'Error> = Result.Error value

    [<Extension>]
    static member Match(result, withOK :Action<_>, withError :Action<_>) =
        result |> ResultEx.bimap (Fun.Of withOK) (Fun.Of withError)

    [<Extension>]
    static member Match(result, withOK :Func<_,_>, withError :Func<_,_>) =
        result |> ResultEx.bimap (Fun.Of withOK) (Fun.Of withError)

    [<Extension>]
    static member IsOK(result) = ResultEx.isOK result

    [<Extension>]
    static member IsError(result) = ResultEx.isError result

    [<Extension>]
    static member Select(result, select) =
        result |> Result.map (Fun.Of<_,_> select)

    [<Extension>]
    static member SelectMany(result, select) =
        result |> Result.bind (Fun.Of<_,_> select)

    [<Extension>]
    static member SelectMany(result, select, merge) =
        result |> Result.bind (fun v ->
            let select = Fun.Of<_,_> select
            let merge = Fun.Of<_,_,_> merge
            v |> select |> Result.map (merge v))
