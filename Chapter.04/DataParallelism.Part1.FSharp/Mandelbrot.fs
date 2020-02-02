namespace DataParallelism.Part1.FSharp

module Mandelbrot =

    open System
    open System.Linq

    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.Advanced
    open SixLabors.ImageSharp.PixelFormats
    
    open System.Collections.Concurrent
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

    let black = new Rgba32((byte) 0, (byte) 0, (byte) 0, Byte.MaxValue)
    let white = new Rgba32(Byte.MaxValue, Byte.MaxValue, Byte.MaxValue, Byte.MaxValue)

    let sequentialMandelbrot size =
        let rows = size
        let cols = size
        
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

        let image = new Image<Rgba32>(rows, cols)
        

        for col = 0 to cols - 1 do
          let pixelRowSpan = image.GetPixelRowSpan(col)
          for  row = 0 to rows - 1 do
            let x,y = colToX row, rowToY col
            let c = Complex(x, y)
            
            let color =
                if isMandelbrot c 100 then black else white
                
            pixelRowSpan.[row] <- color //#E
           
        image


    let parallelMandelbrot size =
        let rows = size
        let cols = size
        
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

        let image = new Image<Rgba32>(rows, cols)
        
        Partitioner.Create(0, cols - 1).AsParallel()
        |> PSeq.withDegreeOfParallelism(Environment.ProcessorCount)
        |> PSeq.withMergeOptions(ParallelMergeOptions.FullyBuffered)
        |> PSeq.iter(fun (s,e) ->
            for col = s to e - 1 do          
              let pixelRowSpan = image.GetPixelRowSpan(col)          
              for  row = 0 to rows - 1 do
                let x,y = colToX row, rowToY col
                let c = Complex(x, y)
                
                let color =
                    if isMandelbrot c 100 then black else white
                    
                pixelRowSpan.[row] <- color //#E
           )
        image




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

    let parallelMandelbrotStruct size =
        let rows = size
        let cols = size
        
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

        let image = new Image<Rgba32>(rows, cols)
        
        Partitioner.Create(0, cols - 1).AsParallel()
        |> PSeq.withDegreeOfParallelism(Environment.ProcessorCount)
        |> PSeq.withMergeOptions(ParallelMergeOptions.FullyBuffered)
        |> PSeq.iter(fun (s,e) ->
            for col = s to e - 1 do
              let pixelRowSpan = image.GetPixelRowSpan(col) 
              for  row = 0 to rows - 1 do
                let x,y = colToX row, rowToY col
                let c = ComplexStruct(x, y)
                
                let color =
                    if isMandelbrot c 100 then black else white
                    
                pixelRowSpan.[row] <- color //#E
           )
        image
