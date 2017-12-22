using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FunctionalTechniques.cs
{
    class Closure
    {
        // Listing 2.5 Closure defined in C# using an anonymous method
        public void FreeVariable()
        {
            string freeVariable = "I am a free variable"; //#A
            Func<string, string> lambda = value => freeVariable + " " + value; //#B
        }

        System.Windows.Controls.Image img;
        // Listing 2.6 Event register with lambda expression capturing local variable
        void UpdateImage(string url)
        {
            System.Windows.Controls.Image image = img; //#A

            var client = new WebClient();
            client.DownloadDataCompleted += (o, e) => //#B
            {
                if (image != null)
                    using (var ms = new MemoryStream(e.Result))
                    {
                        var imageConverter = new ImageSourceConverter();
                        image.Source = (ImageSource) imageConverter.ConvertFrom(ms);
                    }
            };
            client.DownloadDataAsync(new Uri(url)); //#C

            // image = null; //#A from Listing 2.7
        }

        // Listing 2.8 Closure capturing variables in a multi-threaded environment
        public void Closure_Strange_Behavior()
        {
            int iterations = 10;
            for (int i = 1; i <= iterations; i++)
            {
                Task.Factory.StartNew(() =>
                    Console.WriteLine("{0} - {1}", Thread.CurrentThread.ManagedThreadId, i));
            }
        }

        public void Closure_Correct_Behavior()
        {
            int iterations = 10;
            for (int i = 1; i <= iterations; i++)
            {
                var index = i;
                Task.Factory.StartNew(() =>
                    Console.WriteLine("{0} - {1}", Thread.CurrentThread.ManagedThreadId, index));
            }
        }

        // Listing 2.9 Function to calculate the area of a triangle
        void CalcluateAreaFunction()
        {
            Action<int> displayNumber = n => Console.WriteLine(n);

            int i = 5;

            Task taskOne = Task.Factory.StartNew(() => displayNumber(i));

            i = 7;

            Task taskTwo = Task.Factory.StartNew(() => displayNumber(i));

            Task.WaitAll(taskOne, taskTwo);
        }
    }
}