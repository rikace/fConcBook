open System
open System.IO
open System.Threading
open AsyncBlobCloud.FSharp
open Listing
open CodeSnippets

[<RequireQualifiedAccess>]
module Async =

    open Functional.FSharp    
    open Functional.FSharp.AsyncOperators

    let inline map (func:'a -> 'b) (operation:Async<'a>) =
        async {
            let! result = operation
            return func result
        }

    let inline tee (fn:'a -> 'b) (x:Async<'a>) = (AsyncEx.map fn x) |> Async.Ignore |> Async.Start; x

    let parallelWithCatchThrottle (selector:Result<'a> -> 'b)
            (throttle:int) (computations:seq<Async<'a>>) = async {
        use semaphore = new SemaphoreSlim(throttle)
        let throttleAsync (operation:Async<'a>) = async {
            try
                do! semaphore.WaitAsync()
                let! result = Async.Catch operation
                return selector (result |> Result.ofChoice)
            finally
                semaphore.Release() |> ignore }
        return! computations
                |> Seq.map throttleAsync
                |> Async.Parallel  }

    let parallelWithThrottle throttle computations =
        parallelWithCatchThrottle id throttle computations


[<EntryPoint>]
let main argv =
    
    let tempImageFolder = @"./TempImageFolder"

    if not(Directory.Exists(tempImageFolder)) then
        Directory.CreateDirectory(tempImageFolder) |> ignore
        
    let di = new DirectoryInfo(tempImageFolder)
    for file in di.GetFiles() do file.Delete()

    let container =
        getCloudBlobContainerAsync()
        |> Async.RunSynchronously
                    
    getListBlobMediaSync container
    |> Seq.map(fun blob -> CodeSnippets.downloadMediaAsync tempImageFolder blob)
    |> Async.parallelWithThrottle 10
    |> Async.Ignore
    |> Async.RunSynchronously

    Console.WriteLine("Completed!!")
    Console.ReadLine() |> ignore
    0