using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuickSort.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            Random rand = new Random((int) DateTime.Now.Ticks);
            int attempts = 5;
            int[][] dataSamples =
                Enumerable.Range(0, attempts)
                    .Select(x =>
                    {
                        var A = new int[1000000];
                        for (int i = 0; i < 1000000; ++i)
                            A[i] = rand.Next();
                        return A;
                    }).ToArray();

            Func<Action<int[]>, Action[]> run = (sortFunc) =>
                dataSamples.Select(data => (Action)(() => sortFunc(data))).ToArray();

            var implementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "Sequential", run(QuickSort.QuickSort_Sequential)),
                    new Tuple<String, Action[]>(
                        "Parallel", run(QuickSort.QuickSort_Parallel)),
                    new Tuple<String, Action[]>(
                        "ParallelWithDepth", run(QuickSort.QuickSort_Parallel_Threshold)),
                };

            Application.Run(
                PerfVis.toChart("C# QuickSort")
                    .Invoke(PerfVis.fromTuples(implementations)));
        }
    }
}
