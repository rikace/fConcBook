module Program

open System.IO
open System
open System.Drawing
open TamingAgentModule

[<AutoOpen>]
module HelperType =
    type ImageInfo = { Path:string; Name:string; Image:Bitmap}

module ImageHelpers =
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

    // Listing 13.18 The TamingAgent in action for image transformation
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


module ``TamingAgent example`` =

    open AsyncEx
    open ImageHelpers

    let loadandApply3dImage imagePath = Async.retn imagePath >>= loadImage >>= apply3D >>= saveImage

    let loadandApply3dImageAgent = TamingAgent<string, string>(2, loadandApply3dImage)

    let _ = loadandApply3dImageAgent.Subscribe(fun imageName -> printfn "Saved image %s - from subscriber" imageName)

    let transformImages() =
        let images = Directory.GetFiles(@".\Images")
        for image in images do
            loadandApply3dImageAgent.Ask(image) |> run (fun imageName -> printfn "Saved image %s - from reply back" imageName)


module ``Composing TamingAgent with Kleisli operator example`` =
    open Kleisli
    open AsyncEx
    open ImageHelpers

    //Listing 13.19 The TamingAgent with Kleisli operator
    let pipe (limit:int) (operation:'a -> Async<'b>) (job:'a) : Async<_> =
        let agent = TamingAgent(limit, operation)
        agent.Ask(job)

    let loadImageAgent = pipe 2 loadImage
    let apply3DEffectAgent = pipe 2 apply3D
    let saveImageAgent = pipe 2 saveImage

    let pipeline = loadImageAgent >=> apply3DEffectAgent >=> saveImageAgent

    let transformImages() =
        let images = Directory.GetFiles(@".\Images")
        for image in images do
            pipeline image |> run (fun imageName -> printfn "Saved image %s" imageName)



[<EntryPoint>]
let main argv =

    ``TamingAgent example``.transformImages()

    ``Composing TamingAgent with Kleisli operator example``.transformImages();

    Console.ReadLine() |> ignore
    0