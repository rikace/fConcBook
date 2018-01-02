using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelRecipes
{
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();

            Parallel.For(0, 1000, (i) =>
            {
                if (i % 5 == 0)
                    Console.WriteLine($"Random number {random.Next()} - Thread Id {Thread.CurrentThread.ManagedThreadId}");
            });

            Console.ReadLine();

            var safeRrandom = new ThreadSafeRandom();

            Parallel.For(0, 1000, (i) =>
            {
                if (i % 5 == 0)
                    Console.WriteLine($"ThreadSafeRandom number {safeRrandom.Next()} - Thread Id {Thread.CurrentThread.ManagedThreadId}");
            });

            Console.ReadLine();
        }
    }
}
