using System;
using System.Collections.Generic;
using System.Linq;

namespace DataParallelism.Part2.CSharp
{
    using static Functional.Functional;

    public static class MapReducePLINQ
    {
        public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce,
            int M, int R)
        {
            return source.AsParallel()
                .WithDegreeOfParallelism(M)
                .SelectMany(map)
                .GroupBy(keySelector)
                .ToList().AsParallel()
                .WithDegreeOfParallelism(R)
                .Select(reduce)
                .ToArray();
        }

        public static IEnumerable<IGrouping<TKey, TMapped>> Map<TSource, TKey, TMapped>(this IList<TSource> source, Func<TSource, IEnumerable<TMapped>> map, Func<TMapped, TKey> keySelector) =>
                    source.AsParallel()
                   .WithDegreeOfParallelism(Environment.ProcessorCount)
                   .SelectMany(map)
                   .GroupBy(keySelector)
                   .ToList();
    }
}