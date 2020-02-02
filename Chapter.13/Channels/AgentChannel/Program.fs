open SixLabors.ImageSharp
open System.IO
open System
open SixLabors.ImageSharp.PixelFormats
open AgentChannel.Channel

type ImageInfo = { Path:string; Name:string; Image:Image<Rgba32>}

[<EntryPoint>]
let main argv =
    
    let convertImageTo3D (image:Image<Rgba32>) =
        let bitmap = image.Clone()
        
        let w,h = bitmap.Width, bitmap.Height
        for x in 20 .. (w-1) do
            for y in 0 .. (h-1) do // #C
                let c1 = bitmap.[x,y]
                let c2 = bitmap.[x - 20,y]
                let color3D = Rgba32(c1.R, c2.G, c2.B)
                bitmap.[x - 20 ,y] <- color3D
        bitmap

    let chanLoadImage = ChannelAgent<string>()
    let chanApply3DEffect = ChannelAgent<ImageInfo>()
    let chanSaveImage = ChannelAgent<ImageInfo>()

    subscribe chanLoadImage (fun image ->
        let bitmap = Image.Load<Rgba32>(image)
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
        let images = Directory.GetFiles(@"../../../../../../Common/Data/Images")
        for image in images do
            chanLoadImage.Send image |> run

    loadImages()

    Console.ReadLine() |> ignore
    0
