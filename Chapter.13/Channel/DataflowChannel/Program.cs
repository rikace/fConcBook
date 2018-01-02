using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataflowChannel
{
    class ImageInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public Bitmap Image { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
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
                    Image = new Bitmap(image)
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

            var images = Directory.GetFiles(@".\Images");
            foreach (var image in images)
                chanLoadImage.Send(image);

            Console.ReadLine();
            TaskPool.Stop();
        }

        private static Bitmap ConvertImageTo3D(Bitmap image)
        {
            var bitmap = (Bitmap)image.Clone();
            for (var x=20; x<=bitmap.Width-1; x++)
                for (var y=0; y<=bitmap.Height-1; y++)
                {
                    var c1 = bitmap.GetPixel(x, y);
                    var c2 = bitmap.GetPixel(x - 20, y);
                    var color3D = Color.FromArgb(c1.R, c2.G, c2.B);
                    bitmap.SetPixel(x - 20, y, color3D);
                }
            return bitmap;
        }
    }
}
