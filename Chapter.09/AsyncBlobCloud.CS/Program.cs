using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

namespace AsyncBlobCloud
{
    class Program
    {
        static void Main(string[] args)
        {
            var photoViewerPath = @"..\..\..\..\Common\PhotoViewer\App\PhotoViewer.exe";
            var tempImageFolder = @"..\..\..\..\Common\PhotoViewer\App\TempImageFolder";

            var currentDir = Environment.CurrentDirectory;
            var photoViewerPathProc = System.IO.Path.Combine(currentDir, photoViewerPath);

            if (File.Exists(photoViewerPathProc))
            {
                if (!Directory.Exists(tempImageFolder)) Directory.CreateDirectory(tempImageFolder);
                DirectoryInfo di = new DirectoryInfo(tempImageFolder);

                foreach (FileInfo file in di.GetFiles())
                    file.Delete();

                Process proc = new Process();
                proc.StartInfo.FileName = photoViewerPathProc;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(photoViewerPathProc);
                proc.StartInfo.Arguments = tempImageFolder;
                proc.Start();

                AsyncBlobCloud asyncBlobCloud = new AsyncBlobCloud();
                //asyncBlobCloud.DownloadMedia(tempImageFolder);
                //asyncBlobCloud.DownloadMediaAsync(tempImageFolder).Wait();
                //asyncBlobCloud.DownloadInParallelAsync(tempImageFolder).Wait();

                asyncBlobCloud.DownloadInParallelExecuteComplete(tempImageFolder).Wait();
                Console.WriteLine("Completed!!");
                Console.ReadLine();
            }
        }
    }
}
