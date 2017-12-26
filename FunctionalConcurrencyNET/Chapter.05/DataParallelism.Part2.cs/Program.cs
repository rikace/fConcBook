using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataParallelism.Part2.CSharp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("WordCount");
            WordsCounterDemo.Demo();

            Demo.PrintSeparator();
            Console.WriteLine("Listing 5.11 A parallel Reduce function implementation using Aggregate");

            int[] source = Enumerable.Range(0, 100000).ToArray();
            int result = source.AsParallel()
                    .Reduce((value1, value2) => value1 + value2);
        }
    }
}
