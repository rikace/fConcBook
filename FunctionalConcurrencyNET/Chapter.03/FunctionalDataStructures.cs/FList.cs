using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentDataStructures
{
    // Listing 3.14 A functional list in C#
    public sealed class FList<T>
    {
        private FList(T head, FList<T> tail)//#A
        {
            Head = head;
            Tail = tail.IsEmpty
                    ? FList<T>.Empty : tail;
            IsEmpty = false;
        }
        private FList() {                   //#B
            IsEmpty = true;
        }
        public T Head { get; }              //#C
        public FList<T> Tail { get; }       //#D
        public bool IsEmpty { get; }        //#E
        public static FList<T> Cons(T head, FList<T> tail)      //#F
        {
            return tail.IsEmpty
                ? new FList<T>(head, Empty)
                : new FList<T>(head, tail);
        }
        public FList<T> Cons(T element)     //#G
        {
            return FList<T>.Cons(element, this);
        }
        public static readonly FList<T> Empty = new FList<T>(); //#H
    }

    public static class FListExtensions
    {
        public static FList<TOut> Select<TIn,TOut>
            (this FList<TIn> list, Func<TIn, TOut> selector)
        {
            return list.IsEmpty
                ? FList<TOut>.Empty
                : list.Tail.Select<TIn, TOut>(selector).Cons(selector(list.Head));
        }

        public static FList<T> Where<T>
            (this FList<T> list, Func<T, bool> predicate)
        {
            if (list.IsEmpty) return list;
            var newTail = list.Tail.Where(predicate);
            return predicate(list.Head)
                ? newTail.Cons(list.Head)
                : newTail;
        }
    }
}
