open System
open KeyPressedEventCombinators



[<EntryPoint>]
let main argv =
    
    let use_observable = false  // change this value to true or false
                                // if the value use_observable is false, then are used the standard .NET event
                                // otherwise are used the the Observable type
                                
    let keyPressEvent = ConsoleKeyPressEvent()                                

    
    if use_observable then
        (new KeyPressedObservableCombinators("reactive", 5000, keyPressEvent.KeysPressObservable))
            .OnKeyDown.Add(fun value ->
                 printfn "Event %A - %s" (DateTime.Now.ToString("MM/dd/yy H:mm:ss")) value)
    else
        (new KeyPressedEventCombinators("reactive", 5000, keyPressEvent.KeysPressEvent))
            .OnKeyDown.Add(fun value ->
               printfn "Observable %A - %s" (DateTime.Now.ToString("MM/dd/yy H:mm:ss")) value)
            
    Console.ReadLine() |> ignore
    0
//  Microsoft.CSharp.Core.targets(59, 5): [MSB4062] The "Microsoft.CodeAnalysis.BuildTasks.Csc" task could not be loaded from the assembly
//  /usr/local/share/dotnet/sdk/3.1.100/Roslyn/Microsoft.Build.Tasks.CodeAnalysis.dll. Assembly with same name is already loaded Confirm
//  that the <UsingTask> declaration is correct, that the assembly and all its dependencies are available, and that the task contains
//  a public class that implements Microsoft.Build.Framework.ITask.