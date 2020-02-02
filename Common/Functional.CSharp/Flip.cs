using System;

namespace Functional.CSharp
{
    public static partial class Functional
    {
        public static Func<T2, Func<T1, R>> Flip<T1, T2, R>(this Func<T1, Func<T2, R>> func)
        {
            return p2 => p1 => func(p1)(p2);
        }

        public static Func<T2, T1, R> Flip<T1, T2, R>(this Func<T1, T2, R> func)
        {
            return (t2, t1) => func(t1, t2);
        }
    }
}