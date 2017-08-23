open GameOfLife
open System
open System.Windows
open System.Threading
open System.Windows.Threading
open System.Windows.Media.Imaging
open GameOfLifeAgent

[<System.STAThread>]
[<EntryPoint>]
let main argv =
    let form = Window(Content=image, Title="Game of Life")
    let application = new Application()

    let ctx = application.Dispatcher
    use dipose = GameOfLifeAgent.run(ctx)
    application.Run(form) |> ignore

    0 // return an integer exit code

