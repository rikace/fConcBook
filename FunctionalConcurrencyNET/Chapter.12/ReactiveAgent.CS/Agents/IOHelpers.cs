using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveAgent.Agents
{
    public static class File
    {
        public static async Task<string[]> ReadAllLinesAsync(string path)
        {

            using (var sourceStream = new FileStream(path,
                FileMode.Open, FileAccess.Read, FileShare.None,
                bufferSize: 4096, useAsync: true))
            using (var reader = new StreamReader(sourceStream))
            {

                var fileText = await reader.ReadToEndAsync();
                return fileText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(contents);
            await WriteAllBytesAsync(path, encodedText);
        }

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (var sourceStream = new FileStream(path,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(bytes, 0, bytes.Length);
            };
        }
    }
}
