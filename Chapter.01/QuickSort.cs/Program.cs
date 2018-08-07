using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace QuickSort.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<QuickSortRunner>();
        }
    }

    public class QuickSortRunner
    {
        Action<Action<int[]>> run;

        public int ItemRange { get; set; } = 1_000_000;

        public QuickSortRunner()
        {
            Random rand = new Random((int) DateTime.Now.Ticks);

            var dataSample = new int[ItemRange];
            for (int i = 0; i < ItemRange; ++i)
            {
                dataSample[i] = rand.Next();
            }

            run = (sortFunc) =>
            {
                sortFunc(dataSample);
            };
        }

        [Benchmark]
        public void QuickSortSequential()
        { 
            run(QuickSort.QuickSort_Sequential);
        }

        [Benchmark]
        public void QuickSortParallel()
        { 
            run(QuickSort.QuickSort_Parallel);
        }

        [Benchmark]
        public void QuickSortParallelThreshold()
        {
            run(QuickSort.QuickSort_Parallel_Threshold);
        }
    }
}
