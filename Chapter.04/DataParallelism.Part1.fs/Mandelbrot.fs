module Mandelbrot

open System
open System.IO
open System.Linq
open System.Drawing
open System.Drawing.Imaging
open System.Collections.Concurrent
open System.Runtime.InteropServices
open FSharp.Collections.ParallelSeq



// Listing 4.1 Complex number object
type Complex(real : float, imaginary : float) =
    member this.Real = real
    member this.Imaginary = imaginary

    member this.Magnitude =
        sqrt(this.Real * this.Real + this.Imaginary * this.Imaginary)

    static member (+) (c1 : Complex, c2 : Complex) =
        new Complex(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary)
    static member (*) (c1 : Complex, c2 : Complex) =
        new Complex(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                    c1.Real * c2.Imaginary + c1.Imaginary * c2.Real)

let parallelMandelbrot() =
    let rows, cols = 2000, 2000
    let center = Complex(-0.75, 0.0)
    let width, height = 2.5, 2.5

    let colToX col = center.Real - width / 2.0 +
                     float(col) * width / float(cols)
    let rowToY row = center.Imaginary - height / 2.0 +
                     float(row) * height / float(rows)

    let isMandelbrot c iterations =
        let rec isMandelbrot (z:Complex) (rep:int) =
            if rep < iterations && z.Magnitude < 2.0 then
                isMandelbrot (z * z + c) (rep + 1)
            else rep = iterations
        isMandelbrot c 0

    let bitmap = new Bitmap(rows, cols, PixelFormat.Format24bppRgb)
    let width, height = bitmap.Width, bitmap.Height
    let bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                     ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)

    let byteCount = bitmapData.Stride * height
    let pixels : byte[] = Array.zeroCreate<byte> byteCount
    let ptrFirstPixel = bitmapData.Scan0

    Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length)

    Partitioner.Create(0, cols - 1).AsParallel()
    |> PSeq.withDegreeOfParallelism(Environment.ProcessorCount)
    |> PSeq.withMergeOptions(ParallelMergeOptions.FullyBuffered)
    |> PSeq.iter(fun (s,e) ->
        for col = s to e - 1 do
          for  row = 0 to rows - 1 do
            let x,y = colToX row, rowToY col
            let c = Complex(x, y)
            let color = if isMandelbrot c 100 then Color.Black else Color.White
            let offset = (col * bitmapData.Stride) + (3 * row)
            pixels.[offset + 0] <- color.B; // Red component
            pixels.[offset + 1] <- color.G; // Green component
            pixels.[offset + 2] <- color.R; // Blue component
       )

    Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length)
    bitmap.UnlockBits(bitmapData)
    bitmap.Clone() :?> Bitmap



[<Struct>]
type ComplexStruct(real : float, imaginary : float) =
    member this.Real = real
    member this.Imaginary = imaginary

    member this.Magnitude =
        sqrt(this.Real * this.Real + this.Imaginary * this.Imaginary)

    static member (+) (c1 : ComplexStruct, c2 : ComplexStruct) =
        new ComplexStruct(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary)
    static member (*) (c1 : ComplexStruct, c2 : ComplexStruct) =
        new ComplexStruct(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                    c1.Real * c2.Imaginary + c1.Imaginary * c2.Real)

let parallelMandelbrotStruct() =
    let rows, cols = 2000, 2000
    let center = ComplexStruct(-0.75, 0.0)
    let width, height = 2.5, 2.5

    let colToX col = center.Real - width / 2.0 +
                     float(col) * width / float(cols)
    let rowToY row = center.Imaginary - height / 2.0 +
                     float(row) * height / float(rows)

    let isMandelbrot c iterations =
        let rec isMandelbrot (z:ComplexStruct) (rep:int) =
            if rep < iterations && z.Magnitude < 2.0 then
                isMandelbrot (z * z + c) (rep + 1)
            else rep = iterations
        isMandelbrot c 0

    let bitmap = new Bitmap(rows, cols, PixelFormat.Format24bppRgb)
    let width, height = bitmap.Width, bitmap.Height
    let bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                     ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)

    let byteCount = bitmapData.Stride * height
    let pixels : byte[] = Array.zeroCreate<byte> byteCount
    let ptrFirstPixel = bitmapData.Scan0

    Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length)

    Partitioner.Create(0, cols - 1).AsParallel()
    |> PSeq.withDegreeOfParallelism(Environment.ProcessorCount)
    |> PSeq.withMergeOptions(ParallelMergeOptions.FullyBuffered)
    |> PSeq.iter(fun (s,e) ->
        for col = s to e - 1 do
          for  row = 0 to rows - 1 do
            let x,y = colToX row, rowToY col
            let c = ComplexStruct(x, y)
            let color = if isMandelbrot c 100 then Color.Blue else Color.White
            let offset = (col * bitmapData.Stride) + (3 * row)
            pixels.[offset + 0] <- color.B; // Red component
            pixels.[offset + 1] <- color.G; // Green component
            pixels.[offset + 2] <- color.R; // Blue component
       )

    Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length)
    bitmap.UnlockBits(bitmapData)
    bitmap.Clone() :?> Bitmap

