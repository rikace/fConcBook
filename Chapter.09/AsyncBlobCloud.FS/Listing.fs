namespace AsyncBlobCloudFS

open Microsoft.WindowsAzure.Storage
open System.IO
open Microsoft.WindowsAzure.Storage.Blob
open System
open System.Threading
open System.Threading.Tasks
open System.Net
open System.Drawing
open FunctionalConcurrency

[<AutoOpen>]
module Helpers =
    let azureConnection = "< Azure Connection >"

    let bufferSize = 0x1000
    let cts = new CancellationTokenSource()

    let getListBlobMedia (container:CloudBlobContainer) =
        let blobs = container.ListBlobs()
        blobs
        |> Seq.map(fun blob ->
            blob.Uri.Segments.[blob.Uri.Segments.Length - 1])


module CodeSnippets =
    open ImageProcessing
    open FunctionalConcurrency.AsyncOperators

    //Listing 9.1 Asynchronous-Workflow implementation of image-download
    let getCloudBlobContainerAsync() : Async<CloudBlobContainer> = async {
        let storageAccount = CloudStorageAccount.Parse(azureConnection) // #A
        let blobClient = storageAccount.CreateCloudBlobClient() // #B
        let container = blobClient.GetContainerReference("media") // #C
        let! _ = container.CreateIfNotExistsAsync() // #D
        return container }

    let downloadMediaAsync(fileNameDestination:string) (blobNameSource:string) =
      async {   // #E
        let! container = getCloudBlobContainerAsync()   // #F
        let blockBlob = container.GetBlockBlobReference(blobNameSource)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()  // #F

        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous)
        let buffer = Array.zeroCreate<byte> (int blockBlob.Properties.Length)
        let rec copyStream bytesRead = async {
            match bytesRead with
            | 0 -> fileStream.Close()
                   blobStream.Close()
            | n -> do! fileStream.AsyncWrite(buffer, 0, n)   // #F
                   let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
                   return! copyStream bytesRead }

        let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length) // #F
        do! copyStream bytesRead  }


    //Listing 9.2 De-Sugared DownloadMediaAsync computation expression
    let downloadMediaAsyncDeSugar(blobNameSource:string) (fileNameDestination:string) =
        async.Delay(fun() ->    // #A
            async.Bind(getCloudBlobContainerAsync(), fun container ->               // #B
                let blockBlob = container.GetBlockBlobReference(blobNameSource)     // #C
                async.Bind(blockBlob.OpenReadAsync(), fun (blobStream:Stream) ->    // #D
                    let sizeBlob = int blockBlob.Properties.Length
                    async.Bind(blobStream.AsyncRead(sizeBlob), fun bytes ->
                        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous)
                        async.Bind(fileStream.AsyncWrite(bytes, 0, bytes.Length), fun () ->
                            fileStream.Close()
                            blobStream.Close()
                            async.Return()))))) // #E


    // Listing 9.3 AsyncRetry Computation Expression
    type RetryAsyncBuilder(max, sleepMilliseconds : int) =
        let rec retry n (task:Async<'a>) (continuation:'a -> Async<'b>) = async {
            try
                let! result = task  // #A
                let! conResult = continuation result // #B
                return conResult
            with error ->
                if n = 0 then return raise error // #C
                else
                    do! Async.Sleep sleepMilliseconds // #D
                    return! retry (n - 1) task continuation }

        member x.ReturnFrom(f) = f // #E
        member x.Return(v) = async { return v } // #F
        member x.Delay(f) = async { return! f() } // #G
        member x.Bind(task:Async<'a>, continuation:'a -> Async<'b>) =
                                        retry max task continuation // #H
        member x.Bind(task : Task<'T>, continuation : 'T -> Async<'R>) : Async<'R> = x.Bind(Async.AwaitTask task, continuation)


    // Listing 9.4 Retry Async Builder
    let retry = RetryAsyncBuilder(3, 250) // #A

    let downloadMediaCompRetryAsync (blobNameSource:string) (fileNameDestination:string) =
      async {
        let! container = retry { // #B
            return! getCloudBlobContainerAsync() }
        let blockBlob = container.GetBlockBlobReference(blobNameSource)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()

        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous)
        let buffer = Array.zeroCreate<byte> (int blockBlob.Properties.Length)
        let rec copyStream bytesRead = async {
            match bytesRead with
            | 0 -> fileStream.Close(); blobStream.Close()
            | n -> do! fileStream.AsyncWrite(buffer, 0, n)
                   let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
                   return! copyStream bytesRead }

        let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
        do! copyStream bytesRead  }


    // Listing 9.5 Extending the asynchronous workflow to support Task<'a>
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t : Task<'T>, f : 'T -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)
        member x.Bind(t : Task, f : unit -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task) = x.ReturnFrom(Async.AwaitTask computation)


    // Listing 9.6 Async.Parallel downloads all images in parallel
    let downloadMediaCompAsync (container:CloudBlobContainer)
                               (blobMedia:IListBlobItem) = retry { // #B
        let blobName = blobMedia.Uri.Segments.[blobMedia.Uri.Segments.Length-1]
        let blockBlob = container.GetBlockBlobReference(blobName)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()
        return Bitmap.FromStream(blobStream) // #C
    }

    let transormAndSaveImage (container:CloudBlobContainer)
                             (blobMedia:IListBlobItem) =
        downloadMediaCompAsync container blobMedia
        |> AsyncEx.map ImageHelpers.setGrayscale // #D
        |> AsyncEx.map ImageHelpers.createThumbnail // #D
        |> AsyncEx.tee (fun image ->    // #E
                let mediaName =
                    blobMedia.Uri.Segments.[blobMedia.Uri.Segments.Length - 1]
                image.Save(mediaName))

    let downloadMediaCompAsyncParallel() = retry {    // #B
        let! container = getCloudBlobContainerAsync()  // #F
        let computations =
            container.ListBlobs() // #G
            |> Seq.map(transormAndSaveImage container) // #H
        return! Async.Parallel computations } // #I

    type Microsoft.FSharp.Control.Async with
       static member StartCancelable(op:Async<'a>) (tee:'a -> unit)(?onCancel)=
            let ct = new System.Threading.CancellationTokenSource()
            let onCancel = defaultArg onCancel ignore
            Async.StartWithContinuations(op, tee, ignore, onCancel, ct.Token)
            { new IDisposable with
                member x.Dispose() = ct.Cancel() }


    let cancelOperation() =
        downloadMediaCompAsyncParallel()
        |> Async.StartCancelable // #L


    //Listing 9.8 Async.Ignore
    let computation() = async {
        use client = new  WebClient()
        let! manningSite =
             client.AsyncDownloadString(Uri("http://www.manning.com"))
        printfn "Size %d" manningSite.Length
        return manningSite    // #A
    }
    Async.Ignore (computation())  |> Async.Start // #B


    //Listing 9.7 Async.StartWithContinuations
    Async.StartWithContinuations(computation(),             // #A
        (fun site-> printfn "Size %d" site.Length),         // #B
        (fun exn->printfn"exception-%s"<|exn.ToString()),   // #C
        (fun exn->printfn"cancell-%s"<|exn.ToString()))     // #D


    //Listing 9.9 Async.Start
    let computationUnit() = async { // #A
        do! Async.Sleep 1000
        use client = new WebClient()
        let! manningSite =
             client.AsyncDownloadString(Uri("http://www.manning.com"))
        printfn "Size %d" manningSite.Length    // #B
    }
    Async.Start(computationUnit())  // #C


    let getCloudBlobContainer() : CloudBlobContainer =
        let storageAccount = CloudStorageAccount.Parse(azureConnection) // #A
        let blobClient = storageAccount.CreateCloudBlobClient() // #B
        let container = blobClient.GetContainerReference("media") // #C
        let _ = container.CreateIfNotExists()
        container

    //Listing 9.10 Cancellation of an asynchronous computation
    let tokenSource = new CancellationTokenSource()     // #A

    let container = getCloudBlobContainer()
    let parallelComp() =
        container.ListBlobs()
        |> Seq.map(fun blob -> downloadMediaCompAsync container blob)
        |> Async.Parallel

    Async.Start(parallelComp() |> Async.Ignore, tokenSource.Token)  // #A
    tokenSource.Cancel()    // #B


    //Listing 9.11 Cancellation of asynchronous computation with notification
    let onCancelled = fun (cnl:OperationCanceledException) -> // #A
                        printfn "Operation cancelled!"

    //let tokenSource = new CancellationTokenSource()
    let tryCancel = Async.TryCancelled(parallelComp(), onCancelled)  // #B
    Async.Start(tryCancel |> Async.Ignore, tokenSource.Token)


    // Listing 9.12 Async.RunSynchronously
    let computation'() = async {  // #A
        do! Async.Sleep 1000    // #B
        use client = new  WebClient()
        return! client.AsyncDownloadString(Uri("www.manning.com")) // #C
        }
    let manningSite = Async.RunSynchronously(computation'()) // #D
    printfn "Size %d" manningSite.Length // #E



    //Listing 9.13 ParallelWithThrottle and ParallelWithCatchThrottle
    type Result<'a> = Result<'a, exn>   // #A

    module Result =
        let ofChoice value =            // #B
            match value with
            | Choice1Of2 value -> Ok value
            | Choice2Of2 e -> Error e

    module Async =
        let parallelWithCatchThrottle (selector:Result<'a> -> 'b)   // #C
                                      (throttle:int)    // #D
                                      (computations:seq<Async<'a>>) = async {   // #E
            use semaphore = new SemaphoreSlim(throttle)     // #F
            let throttleAsync (operation:Async<'a>) = async {  // #G
                try
                    do! semaphore.WaitAsync()
                    let! result = Async.Catch operation     // #H
                    return selector (result |> Result.ofChoice)  // #I
                finally
                    semaphore.Release() |> ignore } // #L
            return! computations
                    |> Seq.map throttleAsync
                    |> Async.Parallel
        }

        let parallelWithThrottle throttle computations =
            parallelWithCatchThrottle id throttle computations


    //Listing 9.14 ParallelWithThrottle in action with Azure Table Storage downloads
    let maxConcurrentOperations = 100   // #A
    ServicePointManager.DefaultConnectionLimit <- maxConcurrentOperations // #B

    let downloadMediaCompAsyncParallelThrottle() = async {
        let! container = getCloudBlobContainerAsync()
        let computations =
          container.ListBlobs() // #C
          |> Seq.map(fun blobMedia -> transormAndSaveImage container blobMedia)

        return! Async.parallelWithThrottle  // #D

    maxConcurrentOperations computations }