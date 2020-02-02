using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FunctionalTechniques.CSharp
{
    public static partial class Memoization
    {
        public static Func<T, R> MemoizeWeakWithTtl<T, R>(Func<T, R> func, TimeSpan ttl)
            where T : class, IEquatable<T>
            where R : class
        {
            var keyStore = new ConcurrentDictionary<int, T>();

            T ReduceKey(T obj)
            {
                var oldObj = keyStore.GetOrAdd(obj.GetHashCode(), obj);
                return obj.Equals(oldObj) ? oldObj : obj;
            }

            var cache = new ConditionalWeakTable<T, Tuple<R, DateTime>>();

            Tuple<R, DateTime> FactoryFunc(T key)
            {
                return new Tuple<R, DateTime>(func(key), DateTime.Now + ttl);
            }

            return arg =>
            {
                var key = ReduceKey(arg);
                var value = cache.GetValue(key, FactoryFunc);
                if (value.Item2 >= DateTime.Now)
                    return value.Item1;
                value = FactoryFunc(key);
                cache.Remove(key);
                cache.Add(key, value);
                return value.Item1;
            };
        }


        public static void Example()
        {
            string Greeting(string name)
            {
                return $"Warm greetings {name}, the time is {DateTime.Now:hh:mm:ss}";
            }


            var greetingMemoize = MemoizeWeakWithTtl<string, string>(Greeting, TimeSpan.FromDays(1.0));

            Console.WriteLine(greetingMemoize("Richard"));
            Thread.Sleep(1500);
            Console.WriteLine(greetingMemoize("_Richard".Substring(1)));
        }
    }
}