using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentDataStructures
{
    // Listing 3.15 Lazy list implementation using C#
    public sealed class LazyList<T>
    {
        public LazyList(T head, Lazy<LazyList<T>> tail)
        {
            Head = head;
            Tail = tail;
            IsEmpty = false;
        }
        private LazyList()
        {
            IsEmpty = true;
        }
        public T Head { get; }
        public Lazy<LazyList<T>> Tail { get; }
        public bool IsEmpty { get; }

        public static readonly Lazy<LazyList<T>> Empty =
            new Lazy<LazyList<T>>(() => new LazyList<T>());
    }

    public static class LazyListExtensions
    {
        public static LazyList<T> Append<T>
            (this LazyList<T> list, LazyList<T> items)
        {
            if (items.IsEmpty) return list;
            return new LazyList<T>(items.Head,
                new Lazy<LazyList<T>>(() =>
                            list.Append(items.Tail.Value)));
        }

        public static void Iterate<T>
            (this LazyList<T> list, Action<T> action)
        {
            if (list.IsEmpty)
                return;
            action(list.Head);
            list.Tail.Value.Iterate(action);
        }
    }
}
