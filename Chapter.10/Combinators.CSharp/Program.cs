using System;
using System.Threading.Tasks;
using Functional.CSharp.FuctionalType;
using static Combinators.CSharp.AsyncIO;

namespace Combinators.CSharp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var imageName = "Bugghina002.jpg";

            var bugghina2 = await DownloadOptionImage(imageName);
            bugghina2.Match(
                () => Console.WriteLine($"Image {imageName} download unsuccessful"),
                image => Console.WriteLine($"Image {imageName} download successful"));

            Console.WriteLine("Press << ENTER >> to terminate");
            Console.ReadLine();
        }
    }
}