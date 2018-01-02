using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    public static partial class Functional
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (T item in ie)
                action(item);
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, IEnumerable<T> other)
            => source.Aggregate<T, Func<IEnumerable<T>, IEnumerable<T>>>(
                x => x, (f, c) => x => f((new[] { c }).Concat(x)))(other);
    }
}
