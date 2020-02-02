using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace FunctionalTechniques.CSharp
{
    internal class Closure
    {
        private Image img;

        // Listing 2.5 Closure defined in C# using an anonymous method
        public void FreeVariable()
        {
            var freeVariable = "I am a free variable"; //#A
            Func<string, string> lambda = value => freeVariable + " " + value; //#B
        }

        // Listing 2.6 Event register with lambda expression capturing local variable
        private void UpdateImage(string url)
        {
            var image = img; //#A

            var client = new WebClient();
            client.DownloadDataCompleted += (o, e) => //#B
            {
                if (image != null)
                    using (var ms = new MemoryStream(e.Result))
                    {
                        image = Image.Load(ms);
                    }
            };
            client.DownloadDataAsync(new Uri(url)); //#C

            // image = null; //#A from Listing 2.7
        }

        // Listing 2.8 Closure capturing variables in a multi-threaded environment
        public void Closure_Strange_Behavior()
        {
            var iterations = 10;
            for (var i = 1; i <= iterations; i++)
                Task.Factory.StartNew(() =>
                    Console.WriteLine("{0} - {1}", Thread.CurrentThread.ManagedThreadId, i));
        }

        public void Closure_Correct_Behavior()
        {
            var iterations = 10;
            for (var i = 1; i <= iterations; i++)
            {
                var index = i;
                Task.Factory.StartNew(() =>
                    Console.WriteLine("{0} - {1}", Thread.CurrentThread.ManagedThreadId, index));
            }
        }

        // Listing 2.9 Function to calculate the area of a triangle
        private void CalculateAreaFunction()
        {
            Action<int> displayNumber = n => Console.WriteLine(n);

            var i = 5;

            var taskOne = Task.Factory.StartNew(() => displayNumber(i));

            i = 7;

            var taskTwo = Task.Factory.StartNew(() => displayNumber(i));

            Task.WaitAll(taskOne, taskTwo);
        }
    }
}