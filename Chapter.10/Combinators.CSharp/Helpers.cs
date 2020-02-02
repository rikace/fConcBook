using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Combinators.CSharp
{
    internal static class Helpers
    {
        public static readonly string Connection = "< Azure Connection >";

        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(
            CancellationToken cancellationToken = default)
        {
            var storageAccount = CloudStorageAccount.Parse(Connection);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("stuff");

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(Func<T1, T2, T3, R> func)
        {
            return a => b => c => func(a, b, c);
        }

        public static Func<T1, Func<T2, Func<T3, Func<T4, R>>>> Curry<T1, T2, T3, T4, R>(
            Func<T1, T2, T3, T4, R> func)
        {
            return a => b => c => d => func(a, b, c, d);
        }
    }
}