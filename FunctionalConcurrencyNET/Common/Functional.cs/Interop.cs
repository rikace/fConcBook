using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    public static class Interop
    {
        public static FSharpFunc<A, Result> ToFSharpFunc<A, Result>(this Func<A, Result> f)
            => FuncConvert.ToFSharpFunc(new Converter<A, Result>(f));

        public static FSharpFunc<Tuple<A1, A2>, Result> ToTupledFSharpFunc<A1, A2, Result>(this Func<A1, A2, Result> f)
            => FuncConvert.ToFSharpFunc(new Converter<Tuple<A1, A2>, Result>(t => f(t.Item1, t.Item2)));

        public static FSharpFunc<Tuple<A1, A2, A3>, Result> ToTupledFSharpFunc<A1, A2, A3, Result>(this Func<A1, A2, A3, Result> f)
            => FuncConvert.ToFSharpFunc(new Converter<Tuple<A1, A2, A3>, Result>(t => f(t.Item1, t.Item2, t.Item3)));
    }
}
