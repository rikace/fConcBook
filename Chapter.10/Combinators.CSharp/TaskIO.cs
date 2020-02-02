using System.IO;
using System.Threading.Tasks;
using Functional.CSharp.Concurrency.Tasks;
using Functional.CSharp.FuctionalType;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;
using static Combinators.CSharp.AsyncIO;

namespace Combinators.CSharp
{
    public class TaskIO
    {
        public static async Task<Image<Rgba32>> BlendImagesFromBlobStorage(string blobReferenceOne,
            string blobReferenceTwo,
            Size size)
        {
            var blendImagesCurried = Curry<Image<Rgba32>, Image<Rgba32>, Size, Image<Rgba32>>(BlendImages);

            var imageBlended =
                TaskEx.Pure(blendImagesCurried)
                    .Apply(DownloadImageAsync(blobReferenceOne))
                    .Apply(DownloadImageAsync(blobReferenceTwo))
                    .Apply(TaskEx.Pure(size));

            return await imageBlended;
        }


        private static async Task<Image<Rgba32>> CreateThumbnailCurry(string blobReference, int maxPixels)
        {
            var toThumbnailCurried = Curry<Image<Rgba32>, int, Image<Rgba32>>(ToThumbnail); //#A

            var thumbnail = await TaskEx.Pure(toThumbnailCurried) //#B
                .Apply(DownloadImageAsync(blobReference)) //#C
                .Apply(TaskEx.Pure(maxPixels)); //#C

            return thumbnail;
        }

        //Listing 10.11 Composing Task<Result<T>> operations in functional style
        private static async Task<Result<byte[]>> ProcessImage(string nameImage, string destinationImage)
        {
            return await ResultExtensions
                .Bind(ResultExtensions.Map(DownloadResultImage(nameImage), async image => await ToThumbnail(image)),
                    async image => await ToByteArray(image))
                .Tap(async bytes => await File.WriteAllBytesAsync(destinationImage, bytes)); // #A
        }

        public static async Task<Image> BlendImagesFromBlobStorageAsync(string blobReferenceOne,
            string blobReferenceTwo,
            Size size)
        {
            var blendImagesCurried = Curry<Image<Rgba32>, Image<Rgba32>, Size, Image>(BlendImages);

            var imageBlended =
                TaskEx.Pure(blendImagesCurried)
                    .Apply(DownloadImageAsync(blobReferenceOne))
                    .Apply(DownloadImageAsync(blobReferenceTwo))
                    .Apply(TaskEx.Pure(size));
            return await imageBlended;
        }
    }
}