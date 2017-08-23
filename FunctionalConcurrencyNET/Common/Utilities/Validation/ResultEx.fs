namespace ResultEx

open System
open AttributeHelpers

[<Qualified; Module(Suffix)>]
module Result =
        // ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
    let bind f x = 
        match x with
        | Ok s -> f s
        | Error f -> Error f
 

[<Qualified; Module(Suffix)>]
module ResultEx =
    // part of F# 4.1
    //[<Struct>]
    //type Result<'T,'TError> =
    //| Ok of ResultValue:'T
    //| Error of ErrorValue:'TError

    let ok value = Result.Ok value

    let error err = Result.Error err

    [<Qualified>]
    module Checked =
        let ok value =
            match box value with
            | null -> error (ArgumentNullException "value" :> exn)
            | :? exn as ex -> error ex
            | _            -> ok value

        let error (ex :#exn) =
            match box ex with
            | null -> error (ArgumentNullException "value" :> exn)
            | _    -> error (ex :> exn)

    let bimap withOK withError result =
        match result with
        | Ok v -> withOK v
        | Error x -> withError x

    let join withOK withError withResult1 withResult2 result =
        match withResult1 result, withResult2 result with
        | Ok v1, Ok v2 -> ok (withOK v1 v2)
        | Ok _ , Error ex
        | Error ex, Ok _  -> error ex
        | Error e1, Error e2 -> error (withError e1 e2)
   
    // Kliesli operator
    let compose withOne withTwo = withOne >> (Result.bind withTwo)

    let apply result withResult =
        join (fun v fn -> fn v) List.append (fun () -> result) (fun () -> withResult) ()

    let map2 withArgs arg1 arg2 = arg1 |> Result.map withArgs |> apply arg2

    let tryCatch attempt = try (attempt >> ok) () with x -> error x

    let traverse withItem items =
        Seq.foldBack
            (fun head tail ->
                (ok (fun h t -> h :: t))
                |> apply (withItem head)
                |> apply tail)
            items
            (ok [])

    let sequence items = traverse id items

    let toOption result = bimap Some (fun _ -> None) result

    let toChoice result = bimap Choice1Of2 Choice2Of2 result

    let ofChoice choice =
        match choice with
        | Choice1Of2 v -> ok v
        | Choice2Of2 x -> error x

    let isOK result = bimap (fun _ -> true) (fun _ -> false) result

    let isError result = bimap (fun _ -> false) (fun _ -> true) result

    /// Lift a two parameter function to use Result parameters
    let lift2 f x1 x2 = 
        let (<!>) = Result.map
        let (<*>) = apply
        f <!> x1 <*> x2

    /// Lift a three parameter function to use Result parameters
    let lift3 f x1 x2 x3 = 
        let (<!>) = Result.map
        let (<*>) = apply
        f <!> x1 <*> x2 <*> x3
        
    module Operators =
        let inline ( >>= ) result withOK = result |> Result.bind withOK
        let inline ( =<< ) withOK result = result |> Result.bind withOK
        let inline ( >=> ) withFirst withNext = compose withFirst withNext
        let inline ( <=< ) withNext withFirst = compose withFirst withNext
        let inline ( <!> ) result withResult = Result.map withResult result
        let inline ( <*> ) withResult result = apply result withResult
        
[<Qualified; Module(Suffix)>]
module Async =
    [<Qualified>]
    module Result =
        // Async<'a> -> Async<Result<'a, 'b>>
        let ok asyncValue = async {
            let! value = asyncValue
            return value |> Result.Ok
        }

        let error asyncValue = async {
            let! value = asyncValue
            return value |> Result.Error
        }

        let bimap withOK withError asyncResult = async {
            let! result = asyncResult
            match result with
            | Result.Ok v -> return! v |> async.Return |> withOK
            | Result.Error x -> return! x |> async.Return |> withError
        }

        let bind withOK asyncResult = bimap withOK error asyncResult

        let map withResult asyncResult = bind (withResult >> ok) asyncResult

        let compose (withOne :Async<'T> -> Async<Result<'U, 'Error>>) (withTwo :Async<'U> -> Async<Result<'V, 'Error>>) = withOne >> (bind withTwo)

        module Operators =
            let inline ( >>= ) result withOK = result |> bind withOK
            let inline ( =<< ) withOK result = result |> bind withOK
            let inline ( >=> ) withFirst withNext = compose withFirst withNext
            let inline ( <=< ) withNext withFirst = compose withFirst withNext
            let inline ( <!> ) result withResult = map withResult result
