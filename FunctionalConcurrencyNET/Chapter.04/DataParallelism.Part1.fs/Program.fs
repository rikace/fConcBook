open System.Windows.Forms
open System.Drawing


[<EntryPoint>]
let main argv =

    printfn "Mandelbrot Performance Comparison"
    let run func = [func >> ignore]
    [
        "F# Parallel"        , run Mandelbrot.parallelMandelbrot
        "F# Parallel Struct" , run Mandelbrot.parallelMandelbrotStruct
    ]
    |> PerfVis.toChart "F# Mandelbrot"
    |> Application.Run


    Demo.printSeparator()
    printfn "Draw Mandelbrot"
    let image = Mandelbrot.parallelMandelbrotStruct()
    let form = new Form(Visible = true, Text = "Mandelbrot",
                        TopMost = true, Size = Size(800,800))
    new PictureBox(Dock = DockStyle.Fill, Image = image,
                   SizeMode = PictureBoxSizeMode.StretchImage)
    |> form.Controls.Add
    Application.Run(form)


    printfn "Prime Sum [0..10^7]"
    let runSum func =
        [fun() ->
            let res = func()
            printfn "Sum = %d" res
        ]
    [
        "F# Sequential"      , runSum PrimeNumbers.sequentialSum
        "F# Parallel"        , runSum PrimeNumbers.parallelSum
        "F# Parallel LINQ"   , runSum PrimeNumbers.parallelLinqSum
    ]
    |> PerfVis.toChart "F# Prime Sum"
    |> Application.Run

    0 // return an integer exit code
