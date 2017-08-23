using System;
using System.Net;
using System.Threading;

namespace Pipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            //Listing 7.1 Spawning Threads and ThreadPool.QueueUserWorkItem
            Action<string> downloadSite = url => {
                var content = new WebClient().DownloadString(url);
                Console.WriteLine($"The size of the web site {url} is {content.Length}");
            };  // #A

            var threadA = new Thread(() => downloadSite("http://www.nasdaq.com"));
            var threadB = new Thread(() => downloadSite("http://www.bbc.com"));

            threadA.Start();
            threadB.Start(); //#B
            threadA.Join();
            threadB.Join();  //#B

            ThreadPool.QueueUserWorkItem(o => downloadSite("http://www.nasdaq.com"));
            ThreadPool.QueueUserWorkItem(o => downloadSite("http://www.bbc.com")); //#C

            Console.ReadLine();
        }
    }
}
