using System;
using System.Linq;
using System.Numerics;
using Utilities;

namespace ParallelFilterMap
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            bool IsPrime(int n)
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int) Math.Floor(Math.Sqrt(n));
                for (var i = 2; i <= boundary; ++i)
                    if (n % i == 0)
                        return false;
                return true;
            }

            BigInteger ToPow(int n)
            {
                return Math.BigMul(n, n);
            }

            var numbers = Enumerable.Range(0, 100000000).ToList();

            BigInteger SeqOperation()
            {
                return numbers.Where(IsPrime).Select(ToPow).Aggregate(BigInteger.Add);
            }

            BigInteger ParallelLinqOperation()
            {
                return numbers.AsParallel().Where(IsPrime).Select(ToPow).Aggregate(BigInteger.Add);
            }

            BigInteger ParallelFilterMapInline()
            {
                return numbers.FilterMap(IsPrime, ToPow).Aggregate(BigInteger.Add);
            }

            Demo.PrintSeparator();

            Console.WriteLine("Square Prime Sum [0..10000000]");
            Func<Func<BigInteger>, Action[]> runSum = func =>
                new Action[]
                {
                    () =>
                    {
                        var result = func();
                        Console.WriteLine($"Sum = {result}");
                    }
                };
            var sumImplementations = new[]
            {
                new Tuple<string, Action[]>(
                    "C# Sequential", runSum(SeqOperation)),
                new Tuple<string, Action[]>(
                    "C# Parallel LINQ", runSum(ParallelLinqOperation)),
                new Tuple<string, Action[]>(
                    "C# Parallel FilterMap inline", runSum(ParallelFilterMapInline))
            };

            Charting.ToChart("C# Square Prime Sum").Invoke(Charting.fromTuples(sumImplementations));
        }
    }
}