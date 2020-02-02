namespace AsyncInterop

open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob
open SixLabors.ImageSharp
open System.IO

module AsyncInteropDownload =

    let azureConnection = "< AZURE CONNECTION >"

    let downloadBlobsSegment (container: CloudBlobContainer) (blobToken: BlobContinuationToken) = async {
        let! blobs = container.ListBlobsSegmentedAsync(blobToken) |> Async.AwaitTask
        
        return!
            blobs.Results            
            |> Seq.map(fun blobMedia -> async {
                let blobName = blobMedia.Uri.Segments.
                                        [blobMedia.Uri.Segments.Length - 1]
                let blockBlob = container.GetBlockBlobReference(blobName)
                use stream = new MemoryStream()
                do! blockBlob.DownloadToStreamAsync(stream) |> Async.AwaitTask
                let image = Image.Load(stream)
                return image })
            |> Async.Parallel
        }
                
    let downloadMediaAsyncParallel containerName : Async<Image []> = async {
        let storageAccount = CloudStorageAccount.Parse(azureConnection)
        let blobClient = storageAccount.CreateCloudBlobClient()
        let container = blobClient.GetContainerReference(containerName)
        
        let imageList = ResizeArray<_>()
        let mutable blobToken = Unchecked.defaultof<BlobContinuationToken>
        
        let! images = downloadBlobsSegment container blobToken
        imageList.AddRange images
        
        while blobToken |> (isNull >>  not) do                    
            let! images = downloadBlobsSegment container blobToken        
            imageList.AddRange images
            
 
        return imageList |> Seq.toArray }

