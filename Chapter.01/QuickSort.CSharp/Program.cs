using System;
using BenchmarkDotNet.Running;
using BenchmarkUtils;

namespace QuickSort.CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var performanceStats = BenchmarkRunner.Run<BenchmarkQuickSort>();
            var summary = Charting.MapSummary(performanceStats);

            Charting.DrawSummaryReport(summary);

            Console.ReadLine();
        }
    }
}