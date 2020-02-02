using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace Functional.CSharp
{
    public static class AsyncInterop
    {
        //  AsyncEx is a wrapper class that wraps up the F# Async Parallel constratc to makes it more C#-friendly.  
        private static FSharpAsyncBuilder async = ExtraTopLevelOperators.DefaultAsyncBuilder;

        public static FSharpAsync<R[]> Parallel<R>(IEnumerable<FSharpAsync<R>> computations)
        {
            return FSharpAsync.Parallel(computations);
        }

        public static Task<R[]> Parallel<R>(IEnumerable<Task<R>> computations)
        {
            return FSharpAsync.StartAsTask(FSharpAsync.Parallel(computations.Select(FSharpAsync.AwaitTask)),
                FSharpOption<TaskCreationOptions>.None, FSharpOption<CancellationToken>.None);
        }
    }
}