using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Functional.Tasks;

namespace Functional.Validation
{
    public static class TaskValidationEx
    {
        // Task
        //    public static Task<Option<R>> Traverse<T, R>
        //       (this Option<T> @this, Func<T, Task<R>> func)
        //       => @this.Match(
        //             None: () => Async((Option<R>)None),
        //             Some: t => func(t).Map(Some)
        //          );

        //    public static Task<Option<R>> TraverseBind<T, R>(this Option<T> @this, Func<T, Task<Option<R>>> func)
        //       => @this.Match(
        //             None: () => Async((Option<R>)None),
        //             Some: t => func(t)
        //          );



        //    // Task
        //    public static Task<Exceptional<R>> Traverse<T, R>
        //       (this Exceptional<T> @this, Func<T, Task<R>> func)
        //       => @this.Match(
        //             Exception: ex => Return<R>(),
        //             Success: res => Task.FromResult(func(res).Fmap(Of<R>))
        //          );

        //    public static Task<Exceptional<R>> TraverseBind<T, R>(this Exceptional<T> @this
        //       , Func<T, Task<Exceptional<R>>> func)
        //       => @this.Match(
        //             Invalid: reasons => Async(Invalid<R>(reasons)),
        //             Valid: t => func(t)
        //          );
        //}
    }
}
