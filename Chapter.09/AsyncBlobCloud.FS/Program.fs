open System
open System.IO
open System.Threading
open System.Diagnostics
open AsyncBlobCloudFS

[<RequireQualifiedAccess>]
module Async =
    open AsyncBlobCloudFS.CodeSnippets
    open FunctionalConcurrency
    open FunctionalConcurrency.AsyncOperators

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
    let photoViewerPath = @"..\..\..\..\Common\PhotoViewer\App\PhotoViewer.exe"
    let tempImageFolder = @"..\..\..\..\Common\PhotoViewer\App\TempImageFolder"

    let currentDir = Environment.CurrentDirectory
    let photoViewerPathProc = System.IO.Path.Combine(currentDir, photoViewerPath)

    if File.Exists(photoViewerPathProc) then
        if not(Directory.Exists(tempImageFolder)) then Directory.CreateDirectory(tempImageFolder) |> ignore
        let di = new DirectoryInfo(tempImageFolder)
        for file in di.GetFiles() do file.Delete()

        let proc = new Process()
        proc.StartInfo.FileName <- photoViewerPathProc
        proc.StartInfo.WorkingDirectory <- Path.GetDirectoryName(photoViewerPathProc)
        proc.StartInfo.Arguments <- tempImageFolder
        proc.Start() |> ignore

        let container = CodeSnippets.getCloudBlobContainerAsync()
                        |> Async.RunSynchronously
        getListBlobMedia container
        |> Seq.map(fun blob -> CodeSnippets.downloadMediaAsync tempImageFolder blob)
        |> Async.parallelWithThrottle 10
        |> Async.Ignore
        |> Async.RunSynchronously

        Console.WriteLine("Completed!!")
        Console.ReadLine() |> ignore
    0