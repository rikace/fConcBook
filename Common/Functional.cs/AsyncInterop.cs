using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interop
{
    public static class AsyncInterop
    {
        //  AsyncEx is a wrapper class that wraps up the F# Async Parallel constratc to makes it more C#-friendly.  
        static FSharpAsyncBuilder async = ExtraTopLevelOperators.DefaultAsyncBuilder;

        public static FSharpAsync<R[]> Parallel<R>(IEnumerable<FSharpAsync<R>> computations) => FSharpAsync.Parallel<R>(computations);

        public static Task<R[]> Parallel<R>(IEnumerable<Task<R>> computations) =>
            FSharpAsync.StartAsTask(FSharpAsync.Parallel<R>(computations.Select(FSharpAsync.AwaitTask)), FSharpOption<TaskCreationOptions>.None, FSharpOption<System.Threading.CancellationToken>.None);
    }
}
