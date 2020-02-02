using System;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkUtils;

namespace DataParallelism.Part2.CSharp
{
    internal class Program
    {
        private static void RunBenchmarkWordsCounter()
        {
            var performanceStats = BenchmarkRunner.Run<WordsCounterDemo>();
            var summary = Charting.MapSummary(performanceStats);

            Charting.DrawSummaryReport(summary);
        }


        public static void Main(string[] args)
        {
            Console.WriteLine("WordCount");

            RunBenchmarkWordsCounter();

            Console.ReadLine();

            Console.WriteLine("Listing 5.11 A parallel Reduce function implementation using Aggregate");

            var source = Enumerable.Range(0, 100000).ToArray();
            var result = source.AsParallel()
                .Reduce((value1, value2) => value1 + value2);

            Console.WriteLine($"The sum of {source.Length - 1} is {result}");
            Console.ReadLine();
        }
    }
}