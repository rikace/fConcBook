module Program

open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Linq
open System.IO
open System
open System.Text.RegularExpressions
open System.Drawing
open System.Threading
open Channel

type ImageInfo = { Path:string; Name:string; Image:Bitmap}

[<EntryPoint>]
let main argv =

    let convertImageTo3D (image:Bitmap) =
        let bitmap = image.Clone() :?> Bitmap
        let w,h = bitmap.Width, bitmap.Height
        for x in 20 .. (w-1) do
            for y in 0 .. (h-1) do // #C
                let c1 = bitmap.GetPixel(x,y)
                let c2 = bitmap.GetPixel(x - 20,y)
                let color3D = Color.FromArgb(int c1.R, int c2.G, int c2.B)
                bitmap.SetPixel(x - 20 ,y,color3D)
        bitmap

    let chanLoadImage = ChannelAgent<string>()
    let chanApply3DEffect = ChannelAgent<ImageInfo>()
    let chanSaveImage = ChannelAgent<ImageInfo>()

    subscribe chanLoadImage (fun image ->
        let bitmap = new Bitmap(image)
        let imageInfo = { Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                          Name = Path.GetFileName(image)
                          Image = bitmap }
        chanApply3DEffect.Send imageInfo |> run)

    subscribe chanApply3DEffect (fun imageInfo ->
        let bitmap = convertImageTo3D imageInfo.Image
        let imageInfo = { imageInfo with Image = bitmap }
        chanSaveImage.Send imageInfo |> run)

    subscribe chanSaveImage (fun imageInfo ->
        printfn "Saving image %s" imageInfo.Name
        let destination = Path.Combine(imageInfo.Path, imageInfo.Name)
        imageInfo.Image.Save(destination))

    let loadImages() =
        let images = Directory.GetFiles(@".\Images")
        for image in images do
            chanLoadImage.Send image |> run

    loadImages()

    Console.ReadLine() |> ignore
    0
