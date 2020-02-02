using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DataflowChannel
{
    internal class ImageInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public Image<Rgba32> Image { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var chanLoadImage = new ChannelAgent<string>();
            var chanApply3DEffect = new ChannelAgent<ImageInfo>();
            var chanSaveImage = new ChannelAgent<ImageInfo>();

            chanLoadImage.Subscribe(image =>
            {
                var imageInfo = new ImageInfo
                {
                    Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Name = Path.GetFileName(image),
                    Image = Image.Load<Rgba32>(image)
                };
                chanApply3DEffect.Send(imageInfo);
            });

            chanApply3DEffect.Subscribe(imageInfo =>
            {
                imageInfo.Image = ConvertImageTo3D(imageInfo.Image);
                chanSaveImage.Send(imageInfo);
            });

            chanSaveImage.Subscribe(imageInfo =>
            {
                Console.WriteLine($"Saving image {imageInfo.Name}");
                var destination = Path.Combine(imageInfo.Path, imageInfo.Name);
                imageInfo.Image.Save(destination);
            });

            var images = Directory.GetFiles(@"../../../../../../Common/Data/Images");
            foreach (var image in images)
                chanLoadImage.Send(image);

            Console.ReadLine();
            TaskPool.Stop();
        }

        private static Image<Rgba32> ConvertImageTo3D(Image<Rgba32> image)
        {
            var bitmap = image.Clone();
            for (var x = 20; x <= bitmap.Width - 1; x++)
            for (var y = 0; y <= bitmap.Height - 1; y++)
            {
                var c1 = bitmap[x, y];
                var c2 = bitmap[x - 20, y];
                var color3D = new Rgba32(c1.R, c2.G, c2.B);
                bitmap[x - 20, y] = color3D;
            }

            return bitmap;
        }
    }
}