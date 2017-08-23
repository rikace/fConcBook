using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<T2, Func<T1, R>> Flip<T1, T2, R>(this Func<T1, Func<T2, R>> func) => p2 => p1 => func(p1)(p2);

        public static Func<T2, T1, R> Flip<T1, T2, R>(this Func<T1, T2, R> func) => (t2, t1) => func(t1, t2);

        public static Func<T, T> Tee<T>(Action<T> act) => x => { act(x); return x; };
        // Pipes the input value in the given Action, i.e. invokes the given Action on the given value.
        // returning the input value. Not really a genuine implementation of pipe, since it combines pipe with Tap.

        public static T PipeTo<T>(this T input, Action<T> func) => Tee(func)(input);
    }
}