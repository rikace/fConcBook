using System;
using System.IO;
using System.Threading.Tasks;

namespace AsyncBlobCloud.CSharp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var tempImageFolder = @"./TempImageFolder";

            if (!Directory.Exists(tempImageFolder)) Directory.CreateDirectory(tempImageFolder);
            var di = new DirectoryInfo(tempImageFolder);

            foreach (var file in di.GetFiles())
                file.Delete();

            var asyncBlobCloud = new AsyncBlobCloud();
            //asyncBlobCloud.DownloadMedia(tempImageFolder);
            //asyncBlobCloud.DownloadMediaAsync(tempImageFolder).Wait();
            //asyncBlobCloud.DownloadInParallelAsync(tempImageFolder).Wait();

            await asyncBlobCloud.DownloadInParallelExecuteComplete(tempImageFolder);

            Console.WriteLine("Completed!!");
            Console.ReadLine();
        }
    }
}