using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeRandom
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var random = new Random();

            Parallel.For(0, 1000, i =>
            {
                if (i % 5 == 0)
                    Console.WriteLine(
                        $"Random number {random.Next()} - Thread Id {Thread.CurrentThread.ManagedThreadId}");
            });

            Console.ReadLine();

            var safeRandom = new ThreadSafeRandom();

            Parallel.For(0, 1000, i =>
            {
                if (i % 5 == 0)
                    Console.WriteLine(
                        $"ThreadSafeRandom number {safeRandom.Next()} - Thread Id {Thread.CurrentThread.ManagedThreadId}");
            });

            Console.ReadLine();
        }
    }
}