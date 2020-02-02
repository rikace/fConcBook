using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AsyncBlobCloud.CSharp
{
    public static class CloudBlockBlobEx
    {
        public static async Task DownloadToFileAsync(CloudBlobContainer container, int bufferSize, string blobName,
            string fileDestination, CancellationTokenSource cts)
        {
            var blockBlob = container.GetBlockBlobReference(blobName);
            using (var blobStream = await blockBlob.OpenReadAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(), new OperationContext(), cts.Token))
            using (var fileStream = new FileStream(fileDestination, FileMode.Create, FileAccess.Write,
                FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous))
            {
                var buffer = new byte[blockBlob.Properties.Length];
                var bytesRead = await blobStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
            }
        }
    }

    internal static class Helpers
    {
        public static readonly string Connection = "< Azure Connection >";
        public static readonly string ContainerName = "< Azure Account >";

        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(
            CancellationToken cancellationToken = default)
        {
            var storageAccount = CloudStorageAccount.Parse(Connection);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ContainerName);

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(),
                new OperationContext(),
                cancellationToken = cancellationToken);
            return container;
        }

        public static CloudBlobContainer GetCloudBlobContainer(
            CancellationToken cancellationToken = default)
        {
            var storageAccount = CloudStorageAccount.Parse(Connection);
            // Create the blob client.
            var blobClient = storageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            var container = blobClient.GetContainerReference(ContainerName);
            // Create the container if it doesn't already exist.
            container.CreateIfNotExistsAsync().Wait(cancellationToken);
            return container;
        }
    }
}