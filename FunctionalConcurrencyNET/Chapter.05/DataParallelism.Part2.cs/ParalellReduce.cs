using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConcurrencyEx;
using DataParallelism.Part2.CSharp;
//using static Utilities.Interop.Funcs;
//using Utilities.Interop;

namespace DataParallelism.Part2.CSharp
{
    using static Functional.Functional;


    public static class ParalellMapReduce
    {
        // Listing 5.11 A parallel Reduce function implementation using Aggregate
        public static TValue Reduce<TValue>(this ParallelQuery<TValue> source, Func<TValue, TValue, TValue> func) =>

            ParallelEnumerable.Aggregate(source, //#A
          (item1, item2) => func(item1, item2)); //#B


        public static TValue Reduce<TValue>(this IEnumerable<TValue> source, TValue seed, Func<TValue, TValue, TValue> reduce) =>
            source.AsParallel()
            .Aggregate(seed, (local, value) => reduce(local, value),
            (overall, local) => reduce(overall, local), overall => overall);

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

        public static Func<Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> MapReduceFunc<TSource, TMapped, TKey, TResult>(
         this IList<TSource> source)
        {
            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, IEnumerable<IGrouping<TKey, TMapped>>> mapFunc = Map;
            Func<IEnumerable<IGrouping<TKey, TMapped>>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> reduceFunc = Reduce<TSource, TKey, TMapped, TResult>;

            // TODO
            var res = Functional.Functional.ToFunc<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, IEnumerable<IGrouping<TKey, TMapped>>>(Map).Curry();

            return mapFunc.Compose(reduceFunc).Partial(source);
        }


        public static TValue Reduce<TValue>(this IEnumerable<TValue> source, TValue seed, Func<TValue, TValue, TValue> reduce, ParallelOptions parallelOptions)
            //Func<int, T> mapOperation, T seed, Func<T, T, T> associativeCommutativeOperation)
        {

            TValue result = seed; // accumulator for final reduction

            // TODO

            // Reduce in parallel
            Parallel.ForEach(source,
                // Initialize each thread with the user-specified seed
                () => seed,
                // Map the current index to a value and aggregate that value into the local reduction
                (item, loop, localResult, acc) => reduce(item, acc),
                // Combine all of the local reductions
                localResult =>
                    reduce(localResult, result));


                //{
                //    Atom.SwapWithCas(result, (value) =>
                //   // lock (obj) result = associativeCommutativeOperation(localResult, result);
                //});

            // Return the final result
            return result;
        }
    }
}
            //if (parallelOptions == null) throw new ArgumentNullException("parallelOptions");
            //if (mapOperation == null) throw new ArgumentNullException("mapOperation");
            //if (associativeCommutativeOperation == null) throw new ArgumentNullException("associativeCommutativeOperation");
            //if (toExclusive < fromInclusive) throw new ArgumentOutOfRangeException("toExclusive");