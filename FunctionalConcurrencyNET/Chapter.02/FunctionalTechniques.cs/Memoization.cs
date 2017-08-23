using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionalTechniques.cs
{
    class Memoization
    {
        // Listing 2.12 A simple example that clarifies how memoization works
        public static Func<T, R> Memoize<T, R>(Func<T, R> func) where T : IComparable //#A
        {
            Dictionary<T, R> cache = new Dictionary<T, R>();    //#B
            return arg =>                                       //#C
            {
                if (cache.ContainsKey(arg))                     //#D
                    return cache[arg];                          //#E
                return (cache[arg] = func(arg));                //#F
            };
        }

        public static Func<T, R> MemoizeThreadSafe<T, R>(Func<T, R> func) where T : IComparable
        {
            ConcurrentDictionary<T, R> cache = new ConcurrentDictionary<T, R>();
            return arg => cache.GetOrAdd(arg, a => func(a));
        }

        // Listing 2.20 Thread-safe memoization function
        public static Func<T, R> MemoizeLazyThreadSafe<T, R>(Func<T, R> func) where T : IComparable
        {
            // Listing 2.20 Thread-safe memoization function
            //ConcurrentDictionary<T, R> cache = new ConcurrentDictionary<T, R>(); //#A
            //return arg => cache.GetOrAdd(arg, a => func(a));

            // Listing 2.21 Thread-Safe Memoization function with safe lazy evaluation
            ConcurrentDictionary<T, Lazy<R>> cache = new ConcurrentDictionary<T, Lazy<R>>();
            return arg => cache.GetOrAdd(arg, a => new Lazy<R>(() => func(a))).Value;
        }



        // Listing 2.14 Greeting example in C#
        public static string Greeting(string name)
        {
            return $"Warm greetings {name}, the time is {DateTime.Now.ToString("hh:mm:ss")}";
        }

        public static void RunDemo()
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
    }
}