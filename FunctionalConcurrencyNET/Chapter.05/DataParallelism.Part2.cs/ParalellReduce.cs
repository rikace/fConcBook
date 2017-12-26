using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataParallelism.Part2.CSharp
{
    using static Functional.Functional;


    public static class ParalellMapReduce
    {
        // Listing 5.11 A parallel Reduce function implementation using Aggregate
        public static TValue Reduce<TValue>(this ParallelQuery<TValue> source, Func<TValue, TValue, TValue> reduce) =>
            ParallelEnumerable.Aggregate(source, //#A
                    (item1, item2) => reduce(item1, item2)); //#B


        public static TValue Reduce<TValue>(this IEnumerable<TValue> source, TValue seed,
            Func<TValue, TValue, TValue> reduce) =>
            source.AsParallel()
                .Aggregate(
                    seed: seed,
                    updateAccumulatorFunc: (local, value) => reduce(local, value),
                    combineAccumulatorsFunc: (overall, local) => reduce(overall, local),
                    resultSelector: overall => overall);

        public static Func<Func<TSource, TSource, TSource>, TSource> Reduce<TSource>(this IEnumerable<TSource> source)
            => func => source.AsParallel().Aggregate((item1, item2) => func(item1, item2));

        public static IEnumerable<IGrouping<TKey, TMapped>> Map<TSource, TKey, TMapped>(this IList<TSource> source, Func<TSource, IEnumerable<TMapped>> map, Func<TMapped, TKey> keySelector) =>
                    source.AsParallel()
              .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                   .WithDegreeOfParallelism(Environment.ProcessorCount)
                   .SelectMany(map)
                   .GroupBy(keySelector)
                   .ToList();

        public static TResult[] Reduce<TSource, TKey, TMapped, TResult>(this IEnumerable<IGrouping<TKey, TMapped>> source, Func<IGrouping<TKey, TMapped>, TResult> reduce) =>
                      source.AsParallel()
                     .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                     .WithDegreeOfParallelism(Environment.ProcessorCount)
                     .Select(reduce).ToArray();
    }
}