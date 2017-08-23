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


        public static TResult[] Reduce<TSource, TKey, TMapped, TResult>(this IEnumerable<IGrouping<TKey, TMapped>> source, Func<IGrouping<TKey, TMapped>, TResult> reduce) => source.AsParallel()
                     .WithDegreeOfParallelism(Environment.ProcessorCount)
                     .Select(reduce).ToArray();

        public static TResult[] MapReduceFunc<TSource, TMapped, TKey, TResult>(
         this IList<TSource> source,
         Func<TSource, IEnumerable<TMapped>> map,
         Func<TMapped, TKey> keySelector,
         Func<IGrouping<TKey, TMapped>, TResult> reduce,
         int M, int R)
        {

            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>,  IEnumerable<IGrouping<TKey, TMapped>>> mapFunc = Map;

            Func<IEnumerable<IGrouping<TKey, TMapped>>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> reduceFunc = Reduce<TSource, TKey, TMapped, TResult>;
            
            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> mapReduceFunc = mapFunc.Compose(reduceFunc);

            return mapReduceFunc(source, map, keySelector, reduce);
        }
    }
}