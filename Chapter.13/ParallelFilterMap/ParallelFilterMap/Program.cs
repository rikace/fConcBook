using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Numerics;

namespace ParallelFilterMap
{
    class Program
    {
        static void Main(string[] args)
        {
            bool IsPrime(int n)
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            }

            BigInteger ToPow(int n) => (BigInteger)Math.BigMul(n, n);

            var numbers = Enumerable.Range(0, 100000000).ToList();

            BigInteger SeqOperation() => numbers.Where(IsPrime).Select(ToPow).Aggregate(BigInteger.Add);
            BigInteger ParallelLinqOperation() => numbers.AsParallel().Where(IsPrime).Select(ToPow).Aggregate(BigInteger.Add);
            BigInteger ParallelFilterMapInline() => numbers.FilterMap(IsPrime, ToPow).Aggregate(BigInteger.Add);

            Demo.PrintSeparator();
            Console.WriteLine("Square Prime Sum [0..10000000]");
            Func<Func<BigInteger>, Action[]> runSum = (func) =>
                new Action[]
                {
                    () =>
                    {
                        var result = func();
                        Console.WriteLine($"Sum = {result}");
                    }
                };
            var sumImplementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "C# Sequential", runSum(SeqOperation)),
                    new Tuple<String, Action[]>(
                        "C# Parallel LINQ", runSum(ParallelLinqOperation)),
                    new Tuple<String, Action[]>(
                        "C# Parallel FilterMap inline", runSum(ParallelFilterMapInline))
                };
            Application.Run(
                PerfVis.toChart("C# Square Prime Sum").Invoke(PerfVis.fromTuples(sumImplementations)));
        }
    }
}
