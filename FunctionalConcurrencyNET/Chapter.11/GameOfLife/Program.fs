module Program

open GameOfLife
open System
open System.Windows
open System.Threading
open System.Windows.Threading

[<STAThread>]
[<EntryPoint>]
let main argv =

    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()
    GC.WaitForPendingFinalizers()

    let totMemory = GC.GetTotalMemory(true)

    SynchronizationContext.SetSynchronizationContext(
        new DispatcherSynchronizationContext(
            Dispatcher.CurrentDispatcher))

    let form = Window(Content=image, Title="Game of Life", Width=800., Height=823.)
    let application = new Application()

    run(SynchronizationContext.Current)

    let totMemory' = GC.GetTotalMemory(true)
    let diff = totMemory' - totMemory
    printf "Used memory %d bytes" diff
    application.Run(form) |> ignore

    0

