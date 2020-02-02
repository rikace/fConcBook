using BenchmarkDotNet.Attributes;

namespace DataParallelism.Part2.CSharp
{
    [MemoryDiagnoser]
    [RPlotExporter]
    [RankColumn]
    public class BenchmarkWordsCounter
    {
        private const string path = "./Shakespeare";

        [Benchmark]
        public void Sequential()
        {
            WordsCounterDemo.WordsCounter(path);
        }

        [Benchmark]
        public void Parallel()
        {
            WordsCounterDemo.WordsPartitioner(path);
        }
    }
}