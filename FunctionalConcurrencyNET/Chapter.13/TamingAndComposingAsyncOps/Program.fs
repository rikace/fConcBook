module TamingAgent

open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Linq
open System.IO
open System
open System.Text.RegularExpressions
open System.Drawing
open System.Threading
open TamingAgentModule

[<AutoOpen>]
module HelperType =
    type ImageInfo = { Path:string; Name:string; Image:Bitmap}

[<EntryPoint>]
let main argv =

    let run cont op = Async.StartWithContinuations(op, cont, (ignore), (ignore))

    let retn x = async { return x }

    let bind (operation:'a -> Async<'b>) (xAsync:Async<'a>) = async {
        let! x = xAsync
        return! operation x }

    let (>>=) (item:Async<'a>) (operation:'a -> Async<'b>) = bind operation item



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

    let loadImage = (fun (imagePath:string) -> async {
        let bitmap = new Bitmap(imagePath)
        return { Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                 Name = Path.GetFileName(imagePath)
                 Image = bitmap } })

    let apply3D = (fun (imageInfo:ImageInfo) -> async {
        let bitmap = convertImageTo3D imageInfo.Image
        return { imageInfo with Image = bitmap } })

    let saveImage = (fun (imageInfo:ImageInfo) -> async {
        printfn "Saving image %s" imageInfo.Name
        let destination = Path.Combine(imageInfo.Path, imageInfo.Name)
        imageInfo.Image.Save(destination)
        return imageInfo.Name})


    let loadandApply3dImage imagePath = retn imagePath >>= loadImage >>= apply3D >>= saveImage

    let loadandApply3dImageAgent = TamingAgent<string, string>(2, loadandApply3dImage)

    loadandApply3dImageAgent.Subsrcibe(fun imageName -> printfn "Save image %s" imageName)


    let loadImages() =
        let images = Directory.GetFiles(@".\Images")
        for image in images do
            loadandApply3dImageAgent.Ask(image) |> run (fun imagePath -> printfn "Complete processing image %s" imagePath)

    loadImages()

    Console.ReadLine() |> ignore
    0
