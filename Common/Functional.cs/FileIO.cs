using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.IO
{
    public static class File
    {
        private const int BUFFER_SIZE = 0x1000;

        // Opens an existing file for asynchronous reading.
        public static FileStream OpenReadAsync(string path, int bufferSize = BUFFER_SIZE) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);

        // Opens an existing file for asynchronous writing
        public static FileStream OpenWriteAsync(string path, int bufferSize = BUFFER_SIZE) => new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, true);

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (FileStream stream = OpenWriteAsync(path))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length)
                    .ContinueWith(task =>
                    {
                        var e = task.Exception;
                        stream.Dispose();
                        if (e != null) throw e;
                    }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
        {
            using (var destStream = new MemoryStream(stream.CanSeek ? (int)stream.Length : 0))
            {
                return await stream.CopyToAsync(destStream).ContinueWith(task =>
                {
                    var bytes = destStream.ToArray();
                    destStream.Dispose();
                    return bytes;
                });
            }
        }

        public static async Task WriteAllTextAsync(string path, string contents) =>
            await Task.Run(() => Encoding.UTF8.GetBytes(contents))
                .ContinueWith(async task => await WriteAllBytesAsync(path, task.Result));
    }
}