using BenchmarkDotNet.Attributes;

namespace DataParallelism.Part1.CSharp
{
    [MemoryDiagnoser]
    [RPlotExporter]
    [RankColumn]
    public class BenchmarkPrimeSum
    {
        public int len;

        [Params(1_000_000, 10_000_000, 100_000_000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            len = N;
        }

        [Benchmark]
        public void Sequential()
        {
            PrimeNumbers.PrimeSumSequential(len);
        }

        [Benchmark]
        public void Parallel()
        {
            PrimeNumbers.PrimeSumParallel(len);
        }

        [Benchmark]
        public void ParallelLINQ()
        {
            PrimeNumbers.PrimeSumParallelLINQ(len);
        }

        [Benchmark]
        public void ParallelThreadLocal()
        {
            PrimeNumbers.PrimeSumParallelThreadLocal(len);
        }
    }

    [MemoryDiagnoser]
    [RPlotExporter]
    [RankColumn]
    public class BenchmarkMandelbrot
    {
        [Params(1000, 2000, 3000)] public int N;

        public int size;

        [GlobalSetup]
        public void Setup()
        {
            size = N;
        }

        [Benchmark]
        public void Sequential()
        {
            Mandelbrot.SequentialMandelbrot(size);
        }

        [Benchmark]
        public void Parallel()
        {
            Mandelbrot.ParallelMandelbrot(size);
        }

        [Benchmark]
        public void ParallelLINQ()
        {
            Mandelbrot.ParallelMandelbrotOversaturation(size);
        }

        [Benchmark]
        public void ParallelThreadLocal()
        {
            Mandelbrot.ParallelStructMandelbrot(size);
        }
    }
}