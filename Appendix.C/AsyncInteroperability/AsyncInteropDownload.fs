namespace AsyncInterop

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices
open AsyncInterop
open Microsoft.WindowsAzure.Storage
open System.IO

module AsyncInteropDownload =

    let azureConnection = "< AZURE CONNECTION >"

    let downloadMediaAsyncParallel containerName = async {
        let storageAccount = CloudStorageAccount.Parse(azureConnection)
        let blobClient = storageAccount.CreateCloudBlobClient()
        let container = blobClient.GetContainerReference(containerName)
        let computations =
            container.ListBlobs()
            |> Seq.map(fun blobMedia -> async {
            let blobName = blobMedia.Uri.Segments.
                                    [blobMedia.Uri.Segments.Length - 1]
            let blockBlob = container.GetBlockBlobReference(blobName)
            use stream = new MemoryStream()
            do! blockBlob.DownloadToStreamAsync(stream) |> Async.AwaitTask
            let image = System.Drawing.Bitmap.FromStream(stream)
            return image })
        return! Async.Parallel computations }

