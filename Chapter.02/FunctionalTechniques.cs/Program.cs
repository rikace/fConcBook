using Functional;
using FunctionalTechniques.cs;
using System;

namespace FunctionalTechniques
{
    class Program
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
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine(Greeting("Paul"));
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine(Greeting("Richard"));

            Console.WriteLine("\nListing 2.15 Greeting example using memoized function");
            var greetingMemoize = Memoization.Memoize<string, string>(Greeting); //#A

            Console.WriteLine(greetingMemoize("Richard"));  //#B
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine(greetingMemoize("Paul"));
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine(greetingMemoize("Richard"));  //#B
        }

        static void Main(string[] args)
        {
            Closure closure = new Closure();
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
