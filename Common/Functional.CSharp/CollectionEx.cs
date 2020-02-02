using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Functional.CSharp
{
    public static partial class Functional
    {
        public static IEnumerable<T> ForAll<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var item in ie)
                action(item);
            return ie;
        }

        public static IEnumerable<T> ForAll<T>(this IEnumerable<T> ie, Func<T, Task> action)
        {
            foreach (var item in ie)
                action(item);
            return ie;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, IEnumerable<T> other)
        {
            return source.Aggregate<T, Func<IEnumerable<T>, IEnumerable<T>>>(
                x => x, (f, c) => x => f(new[] {c}.Concat(x)))(other);
        }
    }
}