using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace AsyncBlobCloud
{
    public class AsyncBlobCloud
    {
        private readonly int bufferSize = 0x1000;

        public void DownloadMedia(string folderPath)
        {
            var container = Helpers.GetCloudBlobContainer();

            foreach (var blob in container.ListBlobs())
            {
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                                            FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                {
                    blockBlob.DownloadToStream(fileStream);
                }
            }
        }

        public async Task DownloadMediaAsync(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);

            foreach (var blob in container.ListBlobs())
            {
                // Retrieve reference to a blob
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                                        FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous))
                {
                    await blockBlob.DownloadToStreamAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    fileStream.Close();
                }
            }
        }

        public async Task DownloadInParallelAsync(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //var container = Helpers.GetCloudBlobContainer();
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
            var blobs = container.ListBlobs();

            // Create a query that, when executed, returns a collection of tasks.
            IEnumerable<Task> tasks =
                blobs.Select(blob =>
                        DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

            // Use ToList to execute the query and start the tasks.
            Task[] downloadTasks = tasks.ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public Task DownloadInParallelExecuteComplete(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(async () =>
            {
                //var container = Helpers.GetCloudBlobContainer();
                var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
                var blobs = container.ListBlobs();

                // ***Create a query that, when executed, returns a collection of tasks.
                IEnumerable<Task> tasks =
                    blobs.Select(blob =>
                            DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

                // ***Use ToList to execute the query and start the tasks.
                List<Task> downloadTasks = tasks.ToList();

                //await Task.WhenAll(tasks).ConfigureAwait(false);

                // ***Add a loop to process the tasks one at a time until none remain.
                while (downloadTasks.Count > 0)
                {
                    // Identify the first task that completes.
                    Task firstFinishedTask = await Task.WhenAny(downloadTasks);

                    // ***Remove the selected task from the list so that you don't
                    // process it more than once.
                    downloadTasks.Remove(firstFinishedTask);

                    // Await the completed task.
                    await firstFinishedTask;

                    // DO SOMETING
                }
            });
        }

        public async Task DownloadInParallelAsyncUnamb(string folderPath)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var container = await Helpers.GetCloudBlobContainerAsync(cts.Token);
            var blobs = container.ListBlobs();

            // ***Create a query that, when executed, returns a collection of tasks.
            IEnumerable<Task> tasks =
                blobs.Select(blob =>
                    DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cts.Token));

            // ***Use ToList to execute the query and start the tasks.
            List<Task> downloadTasks = tasks.ToList();

            //await Task.WhenAll(tasks).ConfigureAwait(false);

            while (downloadTasks.Count > 0)
            {
                // Identify the first task that completes.
                Task firstFinishedTask = await Task.WhenAny(downloadTasks);

                if (firstFinishedTask.IsCompleted && !firstFinishedTask.IsFaulted)
                {
                    // ***Cancel the rest of the downloads. You just want the first one.
                    cts.Cancel();

                    // Await the completed task.
                    await firstFinishedTask;
                    break;
                }
                else
                {
                    downloadTasks.Remove(firstFinishedTask);
                }
            }
        }

        private async Task DownloadMedia(string blobReference, string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
            using (var memStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                await memStream.FlushAsync().ConfigureAwait(false);
                byte[] data = memStream.ToArray();
                using (var fileStream = new FileStream(Path.Combine(folderPath, blobReference),
                                        FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous))
                {
                    await fileStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                }
            }
        }
    }
}