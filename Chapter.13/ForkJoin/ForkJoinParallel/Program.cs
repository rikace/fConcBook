using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForkJoinParallel
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var N = 100000;

            var task = await Enumerable.Range(1, N)
                .ForkJoin<int, long, long>(
                    async x => new[] {(long) x * x},
                    async (state, x) => state + x, 0L);


            Console.WriteLine($"Sum of squares from 1 to {N} = {task}");
            Console.ReadLine();
        }
    }
}