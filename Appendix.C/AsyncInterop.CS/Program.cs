using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncInterop;
using static AsyncInterop.AsyncInteropDownload;

namespace AsyncInterop.CS
{
    class Program
    {
        static async Task DownloadMediaAsync()
        {
            var cts = new CancellationToken();
            var images = await downloadMediaAsyncParallel("MyMedia").AsTask(cts);
        }

        static void Main(string[] args)
        {

        }
    }
}
