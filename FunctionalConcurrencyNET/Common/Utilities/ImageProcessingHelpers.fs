namespace ImageProcessing

module ImageHelpers =

    open System.IO
    open System
    open System.Drawing.Imaging
    open System.Drawing
    open Microsoft.FSharp.NativeInterop

    let toImage (bytes:byte[]) =
        use stream = new MemoryStream(bytes)
        System.Drawing.Image.FromStream(stream, true)

    let toImageAsync (bytes:byte[]) = async {
        use stream = new MemoryStream()
        do! stream.AsyncWrite(bytes,0,bytes.Length)
        stream.Position <- 0L
        return System.Drawing.Image.FromStream(stream, true)
        }

    let toBytes (image:Bitmap) =
        use memStream = new MemoryStream()
        image.Save(memStream, image.RawFormat)
        memStream.ToArray()

    // load a bitmap in array of tuples (x,y,Color)
    let toRgbArray (bmp : Bitmap) =
        [| for y in 0..bmp.Height-1 do
            for x in 0..bmp.Width-1 -> x,y,bmp.GetPixel(x,y) |]

    let imageToRgbArray (image : Image) =
        let bmp = image :?> Bitmap
        [| for y in 0..bmp.Height-1 do
            for x in 0..bmp.Width-1 -> x,y,bmp.GetPixel(x,y) |]

    let convertImageTo3D (sourceImage:string) (destinationImage:string) = // #A
        let bitmap = Bitmap.FromFile(sourceImage) :?> Bitmap    // #B
        let w,h = bitmap.Width, bitmap.Height
        for x in 20 .. (w-1) do
            for y in 0 .. (h-1) do // #C
                let c1 = bitmap.GetPixel(x,y)
                let c2 = bitmap.GetPixel(x - 20,y)
                let color3D = Color.FromArgb(int c1.R, int c2.G, int c2.B)
                bitmap.SetPixel(x - 20 ,y,color3D)
        bitmap.Save(destinationImage, ImageFormat.Jpeg) // #D

    let setRedscale (sourceImage:string) (destinationImage:string) =    // #F
        let bitmap = Bitmap.FromFile(sourceImage) :?> Bitmap    // #B
        let w,h = bitmap.Width, bitmap.Height
        for x = 0 to (w-1) do
            for y = 0 to (h-1) do   // #C
                let c = bitmap.GetPixel(x,y)
                bitmap.SetPixel(x,y, Color.FromArgb(int c.R, 0, 0))
        bitmap.Save(destinationImage, ImageFormat.Jpeg) // #D


    // builds a bitmap instance from an array of tuples
    let toBitmap (a:(int * int * Color)[]) =
        let height = (a |> Array.Parallel.map (fun (x,_,_) -> x) |> Array.max) + 1
        let width = (a |> Array.Parallel.map (fun (_,y,_) -> y) |> Array.max) + 1
        let bmp = new Bitmap(width, height)
        a |> Array.Parallel.iter (fun (x,y,c) -> bmp.SetPixel(x,y,c))
        bmp
    // converts an image to gray scale
    let toGrayScale (a:(int * int * Color)[]) =
        a |> Array.Parallel.map (
            fun (x,y,c : System.Drawing.Color) ->
                let gscale = int((float c.R * 0.3) + (float c.G * 0.59) + (float c.B * 0.11))
                in  x,y,Color.FromArgb(int c.A, gscale, gscale, gscale))


    // Get a Color from RGB values
    let GetColor x  = Color.FromArgb(Convert.ToInt32(int16 (NativePtr.get x 0)) , Convert.ToInt32(int16 (NativePtr.get x 1)) , Convert.ToInt32(int16 (NativePtr.get x 2)))

    let setGrayscale (image:Image) =    // #E
        let bitmap = image.Clone() :?> Bitmap  // #B
        let w,h = bitmap.Width, bitmap.Height
        for x = 0 to (w-1) do
            for y = 0 to (h-1) do  // #C
                let c = bitmap.GetPixel(x,y)
                let gray = int(0.299 * float c.R + 0.587 * float c.G + 0.114 * float c.B)
                bitmap.SetPixel(x,y, Color.FromArgb(gray, gray, gray))
        bitmap

    let createThumbnail (b:Bitmap) =
        let maxPixels = 400.0
        let scaling = if(b.Width > b.Height) then maxPixels / Convert.ToDouble(b.Width)
                        else maxPixels / Convert.ToDouble(b.Height)
        let size = (Convert.ToInt32(Convert.ToDouble(b.Width) * scaling), Convert.ToInt32(Convert.ToDouble(b.Height) * scaling))
        new System.Drawing.Bitmap(b.GetThumbnailImage(fst size, snd size, null, IntPtr.Zero))


    // You can improve this by avoiding Color.FromArgb, and iterating over bytes instead of ints, but I thought this would be more readable for you, and easier to understand as an approach.
    // The general idea is draw the image into a bitmap of known format (32bpp ARGB), and then check whether that bitmap contains any colors.
    // Locking the bitmap's bits allows you to iterate through it's color-data many times faster than using GetPixel, using unsafe code.
    // If a pixel's alpha is 0, then it is obviously GrayScale, because alpha 0 means it's completely opaque. Other than that - if R = G = B, then it is gray (and if they = 255, it is black).
    let isGrayScale(img:Bitmap) = seq {
        for h = 0 to img.Height - 1 do
            for w = 0 to img.Width - 1 do
                let color = img.GetPixel(w, h)
                yield not((color.R <> color.G || color.G <> color.B || color.R <> color.B) && color.A <> 0uy) } |> Seq.forall(id)

    let isGrayScaleAsync (colors:(_ * _ * Color)[]) = async {
        return colors |> Seq.forall(fun (_,_,color) -> not((color.R <> color.G || color.G <> color.B || color.R <> color.B) && color.A <> 0uy))
    }