using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelForkJoin
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: better application of forkJoin?
            Task<long> task = Enumerable.Range(1, 100000)
                    .ForkJoin<int, long, long>(
                        async x => new[] { (long)x * x },
                        async (state, x) => state + x, 0L);

            Console.WriteLine($"Sum of squares = {task.Result}");
            Console.ReadLine();
        }
    }
}
