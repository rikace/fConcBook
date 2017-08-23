namespace FunctionalConcurrency

type AsyncOption<'a> = Async<Option<'a>>

[<RequireQualifiedAccess>]
module Option =
    let ofChoice choice =
        match choice with
        | Choice1Of2 value -> Some value
        | Choice2Of2 _ -> None

[<RequireQualifiedAccess>]
module AsyncOption =
    let handler (operation:Async<'a>) : AsyncOption<'a> = async {
        let! result = Async.Catch operation
        return (Option.ofChoice result)
    }

