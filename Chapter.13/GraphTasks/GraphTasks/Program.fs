open System
open DirectGraphDependencies
open System.Threading.Tasks
open System.Threading

[<EntryPoint>]
let main argv =
    // Helper function to convert F# Async<unit> to Task
    let inline startAsPlainTask (work : Async<unit>) = Task.Run(fun () -> work |> Async.RunSynchronously)

    let action(id, (delay:int)) = Func<Task>(fun () ->
            async {
                printfn "Starting operation %d in Thread Id %d" id Thread.CurrentThread.ManagedThreadId
                do! Async.Sleep delay
            } |> startAsPlainTask)

    let dagAsync = ParallelTasksDAG()

    dagAsync.AddTask(1, action(1, 600), 4, 5);
    dagAsync.AddTask(2, action(2, 200), 5);
    dagAsync.AddTask(3, action(3, 800), 6, 5);
    dagAsync.AddTask(4, action(4, 500), 6);
    dagAsync.AddTask(5, action(5, 450), 7, 8);
    dagAsync.AddTask(6, action(6, 100), 7);
    dagAsync.AddTask(7, action(7, 900));
    dagAsync.AddTask(8, action(8, 700));

    dagAsync.OnTaskCompleted
    |> Observable.add(fun op ->
        Console.ForegroundColor <- ConsoleColor.Magenta
        printfn "Completed %d" op.Id)

    dagAsync.ExecuteTasks()

    Console.ReadLine() |> ignore
    0
