using System;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> func)
            => (T1 a) => (T2 b) => func(a, b);

        public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> func)
            => (T1 a) => (T2 b) => (T3 c) => func(a, b, c);

        public static Func<T1, Func<T2, Func<T3, Func<T4, R>>>> Curry<T1, T2, T3, T4, R>(
            this Func<T1, T2, T3, T4, R> func)
            => (T1 a) => (T2 b) => (T3 c) => (T4 d) => func(a, b, c, d);

        public static Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, R>>>>> Curry<T1, T2, T3, T4, T5, R>(
            this Func<T1, T2, T3, T4, T5, R> func)
            => (T1 a) => (T2 b) => (T3 c) => (T4 d) => (T5 e) => func(a, b, c, d, e);

        public static Func<T1, Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, R>>>>>> Curry<T1, T2, T3, T4, T5, T6, R>(
            this Func<T1, T2, T3, T4, T5, T6, R> func)
            => (T1 a) => (T2 b) => (T3 c) => (T4 d) => (T5 e) => (T6 f) => func(a, b, c, d, e, f);

        public static Func<T1, Func<T2, T3, R>> CurryFirst<T1, T2, T3, R>
           (this Func<T1, T2, T3, R> func) => t1 => (t2, t3) => func(t1, t2, t3);

        public static Func<T, T> Tap<T>(Action<T> act) => x => { act(x); return x; };
    }
}