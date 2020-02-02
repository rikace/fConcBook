using System;
using BenchmarkDotNet.Running;
using BenchmarkUtils;

namespace DataParallelism.Part1.CSharp
{
    internal class Program
    {
        private static void RunBenchmarkPrimeNumberSum()
        {
            var performanceStats = BenchmarkRunner.Run<BenchmarkPrimeSum>();
            var summary = Charting.MapSummary(performanceStats);

            Charting.DrawSummaryReport(summary);
        }

        private static void RunBenchmarkMandelbrot()
        {
            var performanceStats = BenchmarkRunner.Run<BenchmarkMandelbrot>();
            var summary = Charting.MapSummary(performanceStats);

            Charting.DrawSummaryReport(summary);
        }

        [STAThread]
        private static void Main(string[] args)
        {
            RunBenchmarkPrimeNumberSum();
            Console.ReadLine();

            RunBenchmarkMandelbrot();
            Console.ReadLine();
        }
    }
}