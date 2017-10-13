using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelForkJoin
{
    class Program
    {
        static void Main(string[] args)
        {
            int N = 100000;
            Task<long> task = Enumerable.Range(1, N)
                    .ForkJoin<int, long, long>(
                        async x => new[] { (long)x * x },
                        async (state, x) => state + x, 0L);

            Console.WriteLine($"Sum of squares from 1 to {N} = {task.Result}");
            Console.ReadLine();
        }
    }
}
