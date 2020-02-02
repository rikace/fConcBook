namespace Utilities

namespace System

    [<AutoOpen>]
    module LazyEx =
        let force (x: Lazy<'T>) = x.Force()

namespace System.IO

    [<RequireQualifiedAccess>]
    module FileEx =
        open System.Threading.Tasks
        open System.Text

        let ReadAllBytesAsync (path:string) =
            async {
                use fs = new FileStream(path,
                                FileMode.Open, FileAccess.Read, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                let length = int fs.Length
                return! fs.AsyncRead(length)
            } |> Async.StartAsTask

        let WriteAllBytesAsync (path:string, bytes: byte[]) =
            async {
                use fs = new FileStream(path,
                                FileMode.Append, FileAccess.Write, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                do! fs.AsyncWrite(bytes, 0, bytes.Length)
            }

        let WriteAllBytesTask (path:string, bytes: byte[]) =
            WriteAllBytesAsync(path, bytes) |> Async.StartAsTask

        let ReadFromTextFileAsync(path:string) : Task<string> =
            async {
                use fs = new FileStream(path,
                                FileMode.Open, FileAccess.Read, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                let buffer = Array.zeroCreate<byte> 0x1000
                let rec readRec bytesRead (sb:StringBuilder) = async {
                    if bytesRead > 0 then
                        let content = Encoding.Unicode.GetString(buffer,0,bytesRead)
                        let! bytesRead = fs.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                        return! readRec bytesRead (sb.Append(content))
                    else return sb.ToString() }

                let! bytesRead = fs.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                return! readRec bytesRead (StringBuilder())
            }
            |> Async.StartAsTask

        let WriteTextToFileAsync(path:string) (content:string) : Task<unit> =
            async {
                let encodedContent = Encoding.Unicode.GetBytes(content)
                use fs = new FileStream(path,
                                FileMode.Append, FileAccess.Write, FileShare.None,
                                bufferSize= 0x1000, useAsync= true)
                do! fs.WriteAsync(encodedContent,0,encodedContent.Length) |> Async.AwaitTask
            } |> Async.StartAsTask

namespace ImageSharp

open System
open System.Runtime.CompilerServices
open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Advanced
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Advanced
open SixLabors.ImageSharp.Formats
open SixLabors.ImageSharp.Memory
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing

[<Sealed; Extension>]
type ImageExtensions =
    static member SaveImageAsync (path:string, format:IImageEncoder) (image:Image) =
        async {
            use ms = new MemoryStream()
            image.Save(ms, format)
            do! FileEx.WriteAllBytesAsync(path, ms.ToArray())
        } |> Async.StartAsTask

[<AutoOpen>]
module ImageHelpers =
    type SixLabors.ImageSharp.Image with
        member this.SaveImageAsync (stream:Stream, format:IImageEncoder) =
            async { this.Save(stream, format) }

    let toImage (bytes:byte[]) =
        use stream = new MemoryStream(bytes)
        Image.Load<Rgba32>(stream)

    let toImageAsync (bytes:byte[]) = async {
        use stream = new MemoryStream()
        do! stream.AsyncWrite(bytes,0,bytes.Length)
        stream.Position <- 0L
        return Image.Load<Rgba32>(stream)
    }

    let toBytes (image:Image<Rgba32>) =
        use memStream = new MemoryStream()
        image.SaveAsJpeg(memStream)
        memStream.ToArray()

    // load a bitmap in array of tuples (x,y,Color)
    let toRgbArray (bmp : Image<Rgba32>) =
        [| for y in 0..bmp.Height-1 do
            for x in 0..bmp.Width-1 -> x, y, bmp.[x,y] |]

    let imageToRgbArray (image : Image<Rgba32>) =
        let bmp = image.Clone()
        [| for y in 0..bmp.Height-1 do
            for x in 0..bmp.Width-1 -> x,y,bmp.[x,y] |]

    let convertImageTo3D (sourceImage:string) (destinationImage:string) = // #A
        let bitmap = Image.Load<Rgba32>(sourceImage)  // #B
        let w,h = bitmap.Width, bitmap.Height
        for x in 20 .. (w-1) do
            for y in 0 .. (h-1) do // #C
                let c1 = bitmap.[x,y]
                let c2 = bitmap.[x - 20,y]
                let color3D = Rgba32(c1.R, c2.G, c2.B)
                bitmap.[x - 20 ,y] <- color3D
                
        bitmap.Save(destinationImage) // #D

    let setRedscale (sourceImage:string) (destinationImage:string) =    // #F
        let bitmap = Image.Load<Rgba32>(sourceImage)   // #B
        let w,h = bitmap.Width, bitmap.Height
        for x = 0 to (w-1) do
            for y = 0 to (h-1) do   // #C
                let c = bitmap.[x,y]
                bitmap.[x,y] <- Rgba32(c.R, 0uy, 0uy)
        bitmap.Save(destinationImage) // #D


    // builds a bitmap instance from an array of tuples
    let toBitmap (a:(int * int * Rgba32)[]) =
        let height = (a |> Array.Parallel.map (fun (x,_,_) -> x) |> Array.max) + 1
        let width = (a |> Array.Parallel.map (fun (_,y,_) -> y) |> Array.max) + 1
        let bmp = new Image<Rgba32>(width, height)
        a |> Array.Parallel.iter (fun (x,y,c) -> bmp.[x,y] <- c)
        bmp
        
    // converts an image to gray scale
    let toGrayScale (a:(int * int * Rgba32)[]) =
        a |> Array.Parallel.map (
            fun (x,y,c : Rgba32) ->
                let gscale = byte((float c.R * 0.3) + (float c.G * 0.59) + (float c.B * 0.11))
                in  x,y,Rgba32(c.A, gscale, gscale, gscale))


    // Get a Color from RGB values
    //let GetColor x  = Color.FromArgb(Convert.ToInt32(int16 (NativePtr.get x 0)) , Convert.ToInt32(int16 (NativePtr.get x 1)) , Convert.ToInt32(int16 (NativePtr.get x 2)))

    let setGrayscale (image:Image<Rgba32>) =    // #E
        let bitmap = image.Clone()   // #B
        let w,h = bitmap.Width, bitmap.Height
        for x = 0 to (w-1) do
            for y = 0 to (h-1) do  // #C
                let c = bitmap.[x,y]
                let gray = byte(0.299 * float c.R + 0.587 * float c.G + 0.114 * float c.B)
                bitmap.[x,y] <- Rgba32(gray, gray, gray)
        bitmap

    let createThumbnail (b: Image<Rgba32>) =
        let maxPixels = 400.0
        let scaling = if(b.Width > b.Height) then maxPixels / Convert.ToDouble(b.Width)
                        else maxPixels / Convert.ToDouble(b.Height)
        let w, h = (Convert.ToInt32(Convert.ToDouble(b.Width) * scaling), Convert.ToInt32(Convert.ToDouble(b.Height) * scaling))
        let image = b.Clone() :> Image
        image.Mutate(fun x -> x.Resize(w, h) |> ignore)
        image

    // You can improve this by avoiding Color.FromArgb, and iterating over bytes instead of ints, but I thought this would be more readable for you, and easier to understand as an approach.
    // The general idea is draw the image into a bitmap of known format (32bpp ARGB), and then check whether that bitmap contains any colors.
    // Locking the bitmap's bits allows you to iterate through it's color-data many times faster than using GetPixel, using unsafe code.
    // If a pixel's alpha is 0, then it is obviously GrayScale, because alpha 0 means it's completely opaque. Other than that - if R = G = B, then it is gray (and if they = 255, it is black).
    let isGrayScale(img: Image<Rgba32>) = seq {
        for h = 0 to img.Height - 1 do
            for w = 0 to img.Width - 1 do
                let color = img.[w, h]
                yield not((color.R <> color.G || color.G <> color.B || color.R <> color.B) && color.A <> 0uy) } |> Seq.forall(id)

    let isGrayScaleAsync (colors:(_ * _ * Rgba32)[]) = async {
        return colors |> Seq.forall(fun (_,_,color) -> not((color.R <> color.G || color.G <> color.B || color.R <> color.B) && color.A <> 0uy))
    }
    
        
namespace Utilities

[<AutoOpen>]
module Utils =
    open System

    let charDelimiters = [0..256] |> Seq.map(char)|> Seq.filter(fun c -> Char.IsWhiteSpace(c) || Char.IsPunctuation(c)) |> Seq.toArray

    /// Transforms a function by flipping the order of its arguments.
    let inline flip f a b = f b a

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    let inline tap fn x = fn x |> ignore; x

    /// Sequencing operator like Haskell's ($). Has better precedence than (<|) due to the
    /// first character used in the symbol.
    let (^) = (<|)

    /// Given a value, apply a function to it, ignore the result, then return the original value.
    let inline tee fn x = fn x |> ignore; x

    /// Custom operator for `tee`: Given a value, apply a function to it, ignore the result, then return the original value.
    let inline (|>!) x fn = tee fn x

    let force (x: Lazy<'T>) = x.Force()

    /// Safely invokes `.Dispose()` on instances of `IDisposable`
    let inline dispose (d :#IDisposable) = match box d with null -> () | _ -> d.Dispose()

    let is<'T> (x: obj) = x :? 'T

    let delimiters =
            [0..256]
            |> List.map(char)
            |> List.filter(fun c -> Char.IsWhiteSpace(c) || Char.IsPunctuation(c))
            |> List.toArray
           
module Charting =
    open System
    open XPlot.GoogleCharts
    open System.Collections.Generic
    open BenchmarkUtils    
    open PerfUtil.PerTypes
    
    type PerfTestInput  = (string * ((unit->unit) list)) list
    type PerfTestOutput = (string * (PerfResult list)) list
    
    let fromTuples (input:System.Tuple<string,System.Action[]>[]):PerfTestInput =
        input
        |> Array.map (fun (tuple : System.Tuple<string, System.Action[]>) ->
            let (name, actions) = (tuple : System.Tuple<string, System.Action[]>)
            let implementations =
                actions
                |> Array.map (fun action ->
                    fun () -> action.Invoke())
                |> Array.toList
            name, implementations)
        |> Array.toList    
    
    let CreateLineChart values keys title =
        let data = Seq.zip keys values
        let chart = Chart.Line(data)
        chart.WithTitle(title)
        chart
    
    let BuildReport (keys: string seq, degreesOfParallelism:int [], values:List<List<TimeSpan>>, title:string) =
        let labels = ResizeArray<_>()
        let data = ResizeArray<_>()
        
        for i, value in values |> Seq.indexed do
            let lines = value |> Seq.map(fun t -> t.TotalMilliseconds) |> Seq.toList
            data.Add (Seq.zip keys lines)
            labels.Add  (sprintf "dop=%d" degreesOfParallelism.[i])
        
        let options =
            Options(title = title, hAxis = Axis(logScale = true))
            
        Chart.Combo(data, labels)
        |> Chart.WithOptions options
        |> Chart.WithLabels labels
        |> Chart.WithLegend true
        |> Chart.WithSize (800, 800)
        |> Chart.Show
                   
    let CombineAndShowGcCharts (titles:string[]) (perfResults:PerfResult[]) =
        let data, labels =
            Array.zip titles perfResults
            |> Array.map (fun (t,p) ->
                let data = p.GcDelta |> List.mapi (fun i x -> (sprintf "GcDelta[%d]" i), x)
                let labels = p.GcDelta |> List.map (sprintf "%d")
                data, labels
                //Chart.Column(data, Name=t, Labels = labels)
               )
            |> Array.unzip         
        Chart.Combo(data, labels.[0])
        |> Chart.WithTitle "GC usage"
        |> Chart.WithLegend (true)
        |> Chart.Show       
             
    let private buildChart includeGCgeneration (perfResults:PerfTestOutput) =
        let getAverageTimeBy selector =
            perfResults |> List.map (fun (name, perfResults) ->
                let totalTime = perfResults |> List.sumBy selector
                name, totalTime/float(perfResults.Length))

        let getLabels       = List.map (snd >> sprintf "%.3f")
        let elapsedTimeData = getAverageTimeBy (fun x -> x.Elapsed.TotalMilliseconds)            
        let cpuTimeData = getAverageTimeBy (fun x -> x.CpuTime.TotalMilliseconds)
        

        let addGcGeneration displayGCflag charts =
            if displayGCflag then
                let getGcAverageTimeBy n = getAverageTimeBy (fun x -> x.GcDelta.Item n |> float), sprintf "GC %d Gen" n
                let gcColumns =
                    [   getGcAverageTimeBy 0
                        getGcAverageTimeBy 1
                        getGcAverageTimeBy 2 ]
                    |> List.filter(fun (gcInfo,_) -> gcInfo |> List.filter(fun (_,gcGen) -> gcGen > 0.) |> (List.isEmpty >> not))
                    |> List.map(fun (gcInfo,_) -> gcInfo)
                charts@gcColumns               
            else charts 
     
        let data =
            addGcGeneration includeGCgeneration [elapsedTimeData; cpuTimeData]
        let labels = data |> List.map getLabels |> List.concat |> List.distinct
        Chart.Combo(data, labels)
  
    let private execute (perfTestInput:PerfTestInput):PerfTestOutput=
        perfTestInput
        |> List.map (fun (name, implementations) ->
            printfn "---------------------------------"
            printfn "Running '%s' implementation ..." name
            let perfResults =
                implementations
                |> List.mapi (fun i impl ->
                    printfn "----Executing attempt #%d" i
                    let perfResult = PerfUtil.Run(Action(impl))
                    printfn "PerfResult:%A\n" perfResult
                    perfResult
                )
            name, perfResults
        )        
        
        
    let private createChart includeGCgeneration title (perfResults:PerfTestOutput) =
        let chart = buildChart includeGCgeneration perfResults
        chart
        |> Chart.WithLegend(true)
        |> Chart.WithTitle(title)
        |> Chart.Show
    
    let ToChart title =
        execute >> (createChart false title)    

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
              