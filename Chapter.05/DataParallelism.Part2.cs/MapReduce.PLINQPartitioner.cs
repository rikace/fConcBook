using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DataParallelism.Part2.CSharp
{
    public static class MapReducePLINQPartitioner
    {
        public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce,
            int M, int R)
        {
            var partitioner = Partitioner.Create(source, true);

            var mapResults =
                partitioner.AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(M)
                .SelectMany(map)
                .GroupBy(keySelector)
                .ToList().AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(R)
                .Select(reduce)
                .ToArray();

            return mapResults;
        }
    }
}
