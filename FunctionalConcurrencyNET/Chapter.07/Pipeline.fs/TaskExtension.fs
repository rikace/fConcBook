namespace Pipeline

open System.Runtime.CompilerServices
open System.Threading.Tasks

// Listing 7.9 Task Extension in F# to enable Task LINQ-style operators
[<Sealed; Extension; CompiledName("TaskEx")>]
type TaskExtensions =
    // 'T -> M<'T>
    static member Return value : Task<'T> = Task.FromResult<'T> (value) // #A

    // M<'T> * ('T -> M<'U>) -> M<'U>
    static member Bind (input : Task<'T>, binder : 'T -> Task<'U>) : Task<'U> = // #B
        let tcs = new TaskCompletionSource<'U>()    //#C
        input.ContinueWith(fun (task:Task<'T>) ->
            if (task.IsFaulted) then tcs.SetException(task.Exception.InnerExceptions)
            elif (task.IsCanceled) then tcs.SetCanceled()
            else
                try
                    (binder(task.Result)).ContinueWith(fun (nextTask:Task<'U>) ->
                        tcs.SetResult(nextTask.Result)) |> ignore // #D
                with
                | ex -> tcs.SetException(ex)) |> ignore
        tcs.Task

    static member Select (task : Task<'T>, selector : 'T -> 'U) : Task<'U> =
        task.ContinueWith(fun (t:Task<'T>) -> selector(t.Result))

    static member SelectMany(input:Task<'T>, binder:'T -> Task<'I>, projection:'T -> 'I -> 'R): Task<'R> =
        TaskExtensions.Bind(input,
            fun outer -> TaskExtensions.Bind(binder(outer), fun inner ->
                TaskExtensions.Return(projection outer inner))) // #E

    static member SelectMany(input:Task<'T>, binder:'T -> Task<'R>): Task<'R> =
        TaskExtensions.Bind(input,
            fun outer -> TaskExtensions.Bind(binder(outer), fun inner ->
                TaskExtensions.Return(inner))) // #E
