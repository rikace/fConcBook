using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Combinators.cs
{
    internal static class Helpers
    {
        public static readonly string Connection = "< Azure Connection >";

        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Helpers.Connection);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("stuff");

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync(cancellationToken);
            return container;
        }

        public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(Func<T1, T2, T3, R> func)
    => (T1 a) => (T2 b) => (T3 c) => func(a, b, c);

        public static Func<T1, Func<T2, Func<T3, Func<T4, R>>>> Curry<T1, T2, T3, T4, R>(
          Func<T1, T2, T3, T4, R> func)
         => (T1 a) => (T2 b) => (T3 c) => (T4 d) => func(a, b, c, d);
    }
}
