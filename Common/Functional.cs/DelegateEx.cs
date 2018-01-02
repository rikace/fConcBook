using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<Unit> ToFunc(this Action action)
       => () => { action(); return Unit.Default; };

        public static Func<T, Unit> ToFunc<T>(this Action<T> action)
            => t => { action(t); return Unit.Default; };

        public static Func<T1, T2, Unit> ToFunc<T1, T2>(this Action<T1, T2> action)
            => (T1 t1, T2 t2) => { action(t1, t2); return Unit.Default; };

        public static Func<T, R> ToFunc<T, R>(this Func<T, R> f) => f;

        public static Func<T, M, R> ToFunc<T, M, R>(this Func<T, M, R> f) => f;

        public static Func<T, M, R, Z> ToFunc<T, M, R, Z>(this Func<T, M, R, Z> f) => f;
    }
}
