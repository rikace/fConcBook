using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Functional.CSharp.Concurrency.Async;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using ImageExtensions = ImageSharp.ImageExtensions;

namespace Asynchronous.CSharp
{
    using static ImageExtensions;

    public class AsynchronousOperations
    {
        private Func<string, Task<byte[]>> downloadSiteIcon = async domain =>
        {
            var response = await new
                HttpClient().GetAsync($"http://{domain}/favicon.ico");
            return await response.Content.ReadAsByteArrayAsync();
        };

        private void ReadFileBlocking(string filePath, Action<byte[]> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                var buffer = new byte[fileStream.Length];
                var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                process(buffer);
            }
        }

        // Listing 8.2 Read from the file system asynchronously
        private IAsyncResult ReadFileNoBlocking(string filePath, Action<byte[]> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read, 0x1000,
                FileOptions.Asynchronous))
            {
                var buffer = new byte[fileStream.Length];
                var state = Tuple.Create(buffer, fileStream, process);
                return fileStream.BeginRead(buffer, 0, buffer.Length,
                    EndReadCallback, state);
            }
        }

        private void EndReadCallback(IAsyncResult ar)
        {
            var state = ar.AsyncState as Tuple<byte[], FileStream, Action<byte[]>>;
            using (state.Item2)
            {
                state.Item2.EndRead(ar);
            }

            state.Item3(state.Item1);
        }


        private async Task ReadFileNoBlockingAsync(string filePath, Func<byte[], Task> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read, 0x1000,
                FileOptions.Asynchronous))
            {
                var buffer = new byte[fileStream.Length];
                var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                await process(buffer);
            }
        }


        // Listing 8.3 Download an image(icon) from the network asynchronously
        private async Task DownloadIconAsync(string domain, string fileDestination)
        {
            using (var stream = new FileStream(fileDestination,
                FileMode.Create, FileAccess.Write,
                FileShare.Write, 0x1000, FileOptions.Asynchronous))
            {
                await new HttpClient()
                    .GetAsync($"http://{domain}/favicon.ico")
                    .Bind(async content => await
                        content.Content.ReadAsByteArrayAsync())
                    .Map(bytes => Image.Load<Rgba32>(new MemoryStream(bytes))) // # B
                    .Tap(async image => // #C
                        await SaveImageAsync(fileDestination, new JpegEncoder(), image));
            }
        }

        private async Task DownloadIconAsyncLINQ(string domain, string fileDestination)
        {
            using (var stream = new FileStream(fileDestination,
                FileMode.Create, FileAccess.Write, FileShare.Write,
                0x1000, FileOptions.Asynchronous))
            {
                await (from response in new HttpClient()
                        .GetAsync($"http://{domain}/favicon.ico")
                    from bytes in response.Content.ReadAsByteArrayAsync()
                    select stream.WriteAsync(bytes, 0, bytes.Length));
            }
        }

        private void CooperativeCancellation()
        {
            var ctsOne = new CancellationTokenSource(); // #A
            var ctsTwo = new CancellationTokenSource(); // #A
            var ctsComposite =
                CancellationTokenSource.CreateLinkedTokenSource(ctsOne.Token, ctsTwo.Token); // #B

            var ctsCompositeToken = ctsComposite.Token;

            Task.Factory.StartNew(async () =>
            {
                var webClient = new WebClient();
                ctsCompositeToken.Register(() => webClient.CancelAsync());
                await webClient.DownloadStringTaskAsync("http://www.manning.com");
            }, ctsComposite.Token);
        }
    }
}