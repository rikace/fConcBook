using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FunctionalTechniques.cs
{
    public static partial class Memoization
    {
        // Listing 2.12 A simple example that clarifies how memoization works
        public static Func<T, R> Memoize<T, R>(Func<T, R> func) where T : IComparable //#A
        {
            Dictionary<T, R> cache = new Dictionary<T, R>();    //#B
            return arg =>                                       //#C
            {
                if (cache.ContainsKey(arg))                     //#D
                    return cache[arg];                          //#E
                return (cache[arg] = func(arg));                //#F
            };
        }

        // Listing 2.20 Thread-safe memoization function
        public static Func<T, R> MemoizeThreadSafe<T, R>(Func<T, R> func) where T : IComparable
        {
            ConcurrentDictionary<T, R> cache = new ConcurrentDictionary<T, R>();
            return arg => cache.GetOrAdd(arg, a => func(a));
        }

        // Listing 2.21 Thread-Safe Memoization function with safe lazy evaluation
        public static Func<T, R> MemoizeLazyThreadSafe<T, R>(Func<T, R> func) where T : IComparable
        {
            ConcurrentDictionary<T, Lazy<R>> cache = new ConcurrentDictionary<T, Lazy<R>>();
            return arg => cache.GetOrAdd(arg, a => new Lazy<R>(() => func(a))).Value;
        }
    }
}