open System
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

// Listing 7.2 Parallel.Invoke executing multiple heterogeneous tasks
let convertImageTo3D (sourceImage:string) (destinationImage:string) = // #A
    let bitmap : Image<Rgba32> = Image.Load(sourceImage)  // #B
    
    let w,h = bitmap.Width, bitmap.Height
    for x in 20 .. (w-1) do
        for y in 0 .. (h-1) do // #C
            let c1 = bitmap.[x, y] 
            let c2 = bitmap.[x - 20, y]
            let color3D = Rgba32(byte c1.R, byte c2.G, byte c2.B)            
            bitmap.[x - 20 ,y] <- color3D
    bitmap.Save(destinationImage, Formats.Jpeg.JpegEncoder()) // #D

let setGrayscale (sourceImage:string) (destinationImage:string) =    // #E
    let bitmap : Image<Rgba32> = Image.Load(sourceImage)    // #B
    let w,h = bitmap.Width, bitmap.Height
    for x = 0 to (w-1) do
        for y = 0 to (h-1) do  // #C
            let c = bitmap.[x, y]
            let gray = byte(0.299 * float c.R + 0.587 * float c.G + 0.114 * float c.B)
            bitmap.[x, y] <- Rgba32(gray, gray, gray)
    bitmap.Save(destinationImage, Formats.Jpeg.JpegEncoder()) // #D

let setRedscale (sourceImage:string) (destinationImage:string) =    // #F
    let bitmap : Image<Rgba32> = Image.Load(sourceImage)    // #B
    let w,h = bitmap.Width, bitmap.Height
    for x = 0 to (w-1) do
        for y = 0 to (h-1) do   // #C
            let c = bitmap.[x, y]
            bitmap.[x, y] <- Rgba32(byte c.R, byte 0, byte 0)
    bitmap.Save(destinationImage, Formats.Jpeg.JpegEncoder())  // #D


[<EntryPoint>]
let main argv =
    
    let pathCombine file = IO.Path.Combine("../../../../../Common/Data/Images", file)
    
    System.Threading.Tasks.Parallel.Invoke(
        Action(fun () -> convertImageTo3D (pathCombine "MonaLisa.jpg") (pathCombine "MonaLisa3D.jpg")),
        Action(fun () -> setGrayscale (pathCombine "LadyErmine.jpg") (pathCombine "LadyErmineRed.jpg")),
        Action(fun () -> setRedscale (pathCombine "GinevraBenci.jpg") (pathCombine "GinevraBenciGray.jpg")))
 
    Console.ReadLine() |> ignore

    0
