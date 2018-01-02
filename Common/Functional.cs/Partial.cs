using System;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<T2, R> Partial<T1, T2, R>(this Func<T1, T2, R> func, T1 a) => (T2 b) => func(a, b);

        public static Func<T3, R> Partial<T1, T2, T3, R>(this Func<T1, T2, T3, R> func, T1 a, T2 b)
            => (T3 c) => func(a, b, c);

        public static Func<T2, T3, R> Partial<T1, T2, T3, R>(this Func<T1, T2, T3, R> func, T1 a)
            => (T2 b, T3 c) => func(a, b, c);

        public static Func<T2, T3, T4, TR> Partial<T1, T2, T3, T4, TR>(this Func<T1, T2, T3, T4, TR> func, T1 arg)
            => (arg2, arg3, arg4) => func(arg, arg2, arg3, arg4);

        public static Func<T3, T4, TR> Partial<T1, T2, T3, T4, TR>(this Func<T1, T2, T3, T4, TR> func, T1 arg, T2 arg2)
            => (arg3, arg4) => func(arg, arg2, arg3, arg4);

        public static Func<T4, TR> Partial<T1, T2, T3, T4, TR>(this Func<T1, T2, T3, T4, TR> func, T1 arg, T2 arg2,
            T3 arg3)
            => arg4 => func(arg, arg2, arg3, arg4);

        public static Func<T2, T3, T4, T5, TR> Partial<T1, T2, T3, T4, T5, TR>(this Func<T1, T2, T3, T4, T5, TR> func,
            T1 arg) => (arg2, arg3, arg4, arg5) => func(arg, arg2, arg3, arg4, arg5);

        public static Func<T3, T4, T5, TR> Partial<T1, T2, T3, T4, T5, TR>(this Func<T1, T2, T3, T4, T5, TR> func,
            T1 arg, T2 arg2) => (arg3, arg4, arg5) => func(arg, arg2, arg3, arg4, arg5);

        public static Func<T4, T5, TR> Partial<T1, T2, T3, T4, T5, TR>(this Func<T1, T2, T3, T4, T5, TR> func, T1 arg,
            T2 arg2, T3 arg3) => (arg4, arg5) => func(arg, arg2, arg3, arg4, arg5);

        public static Func<T5, TR> Partial<T1, T2, T3, T4, T5, TR>(this Func<T1, T2, T3, T4, T5, TR> func, T1 arg,
            T2 arg2, T3 arg3, T4 arg4) => arg5 => func(arg, arg2, arg3, arg4, arg5);
    }
}
