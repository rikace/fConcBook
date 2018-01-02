namespace FunctionalConcurrency

type Result<'TSuccess> = Result<'TSuccess, exn>

//Listing 10.12 AsyncResult handler to catch and wrap asynchronous computation
module Result =
    let ofChoice value =             // #A
        match value with
        | Choice1Of2 value -> Ok value
        | Choice2Of2 e -> Error e

    let apply fRes xRes =
        match fRes,xRes with
        | Ok f, Ok x -> Ok (f x)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    let defaultValue value result =
        match result with
        | Ok(res) -> res
        | Error(_) -> value

    let bimap success failure =
        function
        | Ok v -> success v
        | Error x -> failure x