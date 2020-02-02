using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace QuickSort.CSharp
{
    [MemoryDiagnoser]
    [RPlotExporter]
    [RankColumn]
    public class BenchmarkQuickSort
    {
        public int[] iterations;
        [Params(1000, 10000)] public int N;

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random((int) DateTime.Now.Ticks);
            iterations = Enumerable.Range(0, N).Select(_ => rand.Next()).ToArray();
        }

        [Benchmark]
        public void Sequential()
        {
            Functions.QuickSort_Sequential(iterations);
        }

        [Benchmark]
        public void Parallel()
        {
            Functions.QuickSort_Parallel(iterations);
        }

        [Benchmark]
        public void ParallelDepth()
        {
            Functions.QuickSort_Parallel_Threshold(iterations);
        }
    }
}