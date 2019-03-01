using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Functional.Async;
using static System.Drawing.ImageExtensions;

namespace ConsoleApplication1
{
    class AsyncOperations
    {
        void ReadFileBlocking(string filePath, Action<byte[]> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                                          FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                process(buffer);
            }
        }

        // Listing 8.2 Read from the file system asynchronously
        IAsyncResult ReadFileNoBlocking(string filePath, Action<byte[]> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                                    FileAccess.Read, FileShare.Read, 0x1000,
                                                     FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[fileStream.Length];
                var state = Tuple.Create(buffer, fileStream, process);
                return fileStream.BeginRead(buffer, 0, buffer.Length,
                                              EndReadCallback, state);
            }
        }

        void EndReadCallback(IAsyncResult ar)
        {
            var state = ar.AsyncState as Tuple<byte[], FileStream, Action<byte[]>>;
            using (state.Item2) state.Item2.EndRead(ar);
            state.Item3(state.Item1);
        }


        async Task ReadFileNoBlockingAsync(string filePath, Func<byte[], Task> process)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open,
                                            FileAccess.Read, FileShare.Read, 0x1000,
                                            FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[fileStream.Length];
                int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                await process(buffer);
            }
        }


        private Func<string, Task<byte[]>> downloadSiteIcon = async domain =>
        {
            var response = await new
                HttpClient().GetAsync($"http://{domain}/favicon.ico");
            return await response.Content.ReadAsByteArrayAsync();
        };
        

        // Listing 8.3 Download an image(icon) from the network asynchronously
        async Task DownloadIconAsync(string domain, string fileDestination)
        {
            using (FileStream stream = new FileStream(fileDestination,
                FileMode.Create, FileAccess.Write,
                FileShare.Write, 0x1000, FileOptions.Asynchronous))
                await new HttpClient()
                    .GetAsync($"http://{domain}/favicon.ico")
                    .Bind(async content => await
                        content.Content.ReadAsByteArrayAsync())
                    .Map(bytes => Image.FromStream(new MemoryStream(bytes))) // # B
                    .Tap(async image => // #C
                               await SaveImageAsync(fileDestination, ImageFormat.Jpeg, image));

        }

        async Task DownloadIconAsyncLINQ(string domain, string fileDestination)
        {
            using (FileStream stream = new FileStream(fileDestination,
                            FileMode.Create, FileAccess.Write, FileShare.Write,
                            0x1000, FileOptions.Asynchronous))
                await (from response in new HttpClient()
                                            .GetAsync($"http://{domain}/favicon.ico")
                       from bytes in response.Content.ReadAsByteArrayAsync()
                       select stream.WriteAsync(bytes, 0, bytes.Length));
        }

        void CooperativeCancellation()
        {
            CancellationTokenSource ctsOne = new CancellationTokenSource(); // #A
            CancellationTokenSource ctsTwo = new CancellationTokenSource();// #A
            CancellationTokenSource ctsComposite = CancellationTokenSource.CreateLinkedTokenSource(ctsOne.Token, ctsTwo.Token); // #B

            CancellationToken ctsCompositeToken = ctsComposite.Token;

            Task.Factory.StartNew(async () =>
            {
                var webClient = new WebClient();
                ctsCompositeToken.Register(() => webClient.CancelAsync());
                await webClient.DownloadStringTaskAsync("http://www.manning.com");
            }, ctsComposite.Token);
        }
    }
}
