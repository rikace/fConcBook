using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AsyncBlobCloud.CSharp
{
    public class AsyncBlobCloud
    {
        private readonly int bufferSize = 0x1000;

        public void DownloadMedia(string folderPath)
        {
            var container = Helpers.GetCloudBlobContainer();

            var blobToken = new BlobContinuationToken();
            foreach (var blob in container.ListBlobsSegmentedAsync(blobToken).Result.Results)
            {
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                var blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                    FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                {
                    blockBlob.DownloadToStreamAsync(fileStream).Wait();
                }
            }
        }

        public async Task DownloadMediaAsync(string folderPath,
            CancellationToken cancellationToken = default)
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);

            var blobToken = new BlobContinuationToken();
            var results = await container.ListBlobsSegmentedAsync(blobToken);
            foreach (var blob in results.Results)
            {
                // Retrieve reference to a blob
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                var blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                    FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous))
                {
                    await blockBlob.DownloadToStreamAsync(fileStream, AccessCondition.GenerateEmptyCondition(),
                        new BlobRequestOptions(), new OperationContext(), cancellationToken).ConfigureAwait(false);
                    fileStream.Close();
                }
            }
        }

        public async Task DownloadInParallelAsync(string folderPath,
            CancellationToken cancellationToken = default)
        {
            //var container = Helpers.GetCloudBlobContainer();
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
            var blobToken = new BlobContinuationToken();
            var blobsResults = await container.ListBlobsSegmentedAsync(blobToken);

            // Create a query that, when executed, returns a collection of tasks.
            var tasks =
                blobsResults.Results.Select(blob =>
                    DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

            // Use ToList to execute the query and start the tasks.
            var enumerableTasks = tasks as Task[] ?? tasks.ToArray();
            var downloadTasks = enumerableTasks.ToArray();

            await Task.WhenAll(downloadTasks).ConfigureAwait(false);
        }

        public Task DownloadInParallelExecuteComplete(string folderPath,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                //var container = Helpers.GetCloudBlobContainer();
                var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
                var blobToken = new BlobContinuationToken();
                var blobsResults = await container.ListBlobsSegmentedAsync(blobToken);

                // ***Create a query that, when executed, returns a collection of tasks.
                var tasks =
                    blobsResults.Results.Select(blob =>
                        DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

                // ***Use ToList to execute the query and start the tasks.
                var downloadTasks = tasks.ToList();

                //await Task.WhenAll(tasks).ConfigureAwait(false);

                // ***Add a loop to process the tasks one at a time until none remain.
                while (downloadTasks.Count > 0)
                {
                    // Identify the first task that completes.
                    var firstFinishedTask = await Task.WhenAny(downloadTasks);

                    // ***Remove the selected task from the list so that you don't
                    // process it more than once.
                    downloadTasks.Remove(firstFinishedTask);

                    // Await the completed task.
                    await firstFinishedTask;

                    // DO SOMETHING
                }
            });
        }

        public async Task DownloadInParallelAsyncUnamb(string folderPath)
        {
            var cts = new CancellationTokenSource();
            var container = await Helpers.GetCloudBlobContainerAsync(cts.Token);
            var blobToken = new BlobContinuationToken();
            var blobsResults = await container.ListBlobsSegmentedAsync(blobToken);

            // ***Create a query that, when executed, returns a collection of tasks.
            var tasks =
                blobsResults.Results.Select(blob =>
                    DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cts.Token));

            // ***Use ToList to execute the query and start the tasks.
            var downloadTasks = tasks.ToList();

            //await Task.WhenAll(tasks).ConfigureAwait(false);

            while (downloadTasks.Count > 0)
            {
                // Identify the first task that completes.
                var firstFinishedTask = await Task.WhenAny(downloadTasks);

                if (firstFinishedTask.IsCompleted && !firstFinishedTask.IsFaulted)
                {
                    // ***Cancel the rest of the downloads. You just want the first one.
                    cts.Cancel();

                    // Await the completed task.
                    await firstFinishedTask;
                    break;
                }

                downloadTasks.Remove(firstFinishedTask);
            }
        }

        private async Task DownloadMedia(string blobReference, string folderPath,
            CancellationToken cancellationToken = default)
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blockBlob = container.GetBlockBlobReference(blobReference);
            using (var memStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                await memStream.FlushAsync().ConfigureAwait(false);
                var data = memStream.ToArray();
                using (var fileStream = new FileStream(Path.Combine(folderPath, blobReference),
                    FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous))
                {
                    await fileStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                }
            }
        }
    }
}