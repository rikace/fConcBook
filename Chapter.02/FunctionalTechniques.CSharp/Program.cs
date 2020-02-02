using System;
using System.Threading;
using Utilities;

namespace FunctionalTechniques.CSharp
{
    internal class Program
    {
        // Listing 2.14 Greeting example in C#
        public static string Greeting(string name)
        {
            return $"Warm greetings {name}, the time is {DateTime.Now.ToString("hh:mm:ss")}";
        }

        public static void RunDemoMemoization()
        {
            Console.WriteLine("Listing 2.14 Greeting example in C#");
            Console.WriteLine(Greeting("Richard"));
            Thread.Sleep(2000);
            Console.WriteLine(Greeting("Paul"));
            Thread.Sleep(2000);
            Console.WriteLine(Greeting("Richard"));

            Console.WriteLine("\nListing 2.15 Greeting example using memoized function");
            var greetingMemoize = Memoization.Memoize<string, string>(Greeting); //#A

            Console.WriteLine(greetingMemoize("Richard")); //#B
            Thread.Sleep(2000);
            Console.WriteLine(greetingMemoize("Paul"));
            Thread.Sleep(2000);
            Console.WriteLine(greetingMemoize("Richard")); //#B
        }

        private static void Main(string[] args)
        {
            var closure = new Closure();
            closure.Closure_Strange_Behavior();
            Demo.PrintSeparator();
            closure.Closure_Correct_Behavior();
            Demo.PrintSeparator();

            RunDemoMemoization();
            Demo.PrintSeparator();

            WebCrawlerExample.RunDemo();
            Demo.PrintSeparator();

            ConcurrentSpeculation.FuzzyMatchDemo();
            Demo.PrintSeparator();

            Person.RunDemo();
        }
    }
}