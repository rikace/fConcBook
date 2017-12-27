using System;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<T1, TR> Compose<T1, T2, TR>(this Func<T1, T2> f1, Func<T2, TR> f2) => v => f2(f1(v));

        public static Func<T1, T2, TR> Compose<T1, T2, T3, TR>(this Func<T1, T2, T3> f1, Func<T3, TR> f2)
            => (a, b) => f2(f1(a, b));

        public static Func<T1, T2, T3, TR> Compose<T1, T2, T3, T4, TR>(this Func<T1, T2, T3, T4> f1, Func<T4, TR> f2)
            => (a, b, c) => f2(f1(a, b, c));

        public static Func<T1, TR> Compose<T1, T2, T3, TR>(
            Func<T1, T2> f1, Func<T2, T3> f2, Func<T3, TR> f3) => arg => f3(f2(f1(arg)));

        public static Func<T1, T2, TR> Compose<T1, T2, T3, T4, TR>(this Func<T1, T2, T2> f1, Func<T2, T3> f2,
            Func<T3, TR> f3) => (a, b) => f3(f2(f1(a, b)));

        public static Func<T1, T2, T3, TR> Compose<T1, T2, T3, T4, T5, TR>(
            this Func<T1, T2, T3, T4> f1, Func<T4, T5> f2, Func<T5, TR> f3) => (a, b, c) => f3(f2(f1(a, b, c)));

        public static Func<T1, T2, T3, T4, TR> Compose<T1, T2, T3, T4, T5, T6, TR>(
            this Func<T1, T2, T3, T4, T5> f1, Func<T5, T6> f2, Func<T6, TR> f3)
            => (a, b, c, d) => f3(f2(f1(a, b, c, d)));

        public static Func<T1, T2, T3, T5, TR> Compose<T1, T2, T3, T4, T5, TR>(this Func<T1, T2, T3, T4> f1, Func<T4, T5, TR> f2)
          => (a, b, c, d) => f2(f1(a, b, c), d);

        public static Action<T1> Compose<T1, T2>(this Func<T1, T2> func, Action<T2> action) => arg => action(func(arg));
    }
}