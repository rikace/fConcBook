using Functional;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Functional.OptionHelpers;
using Functional.Tasks;
using System.Drawing.Drawing2D;
using Functional.Async;

namespace Combinators.cs
{
    internal static class Helpers
    {
        public static readonly string Connection = "< Azure Connection >";

        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Helpers.Connection);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("stuff");

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync(cancellationToken);
            return container;
        }

    }

    class Program
    {
        //Listing 10.1 DownloadImage with traditional imperative error handling
        static async Task<Image> DownloadImage(string blobReference)
        {
            try
            {
                var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false); // #A
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);   // #A
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);  // #A
                    return Bitmap.FromStream(memStream);
                }
            }
            catch (StorageException ex)
            {
                Log.Error("Azure Storage error", ex);  // #B
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Some general error", ex);  // #B
                throw;
            }
        }

        static async Task RunDownloadImage()   // #C
        {
            try
            {
                var image = await DownloadImage("Bugghina0001.jpg");
                ProcessImage(image);
            }
            catch (Exception ex)
            {
                HandlingError(ex);  // #B
                throw;
            }

            //Image image = await AsyncEx.Retry(
            //            () => DownloadImage("Bugghina001.jpg")
            //                    .Otherwise(() => DownloadImage("Bugghina002.jpg")),
            //            5, TimeSpan.FromSeconds(2));
        }

        private static void HandlingError(Exception ex)
        {
            throw new NotImplementedException();
        }

        private static void ProcessImage(Image image)
        {
            throw new NotImplementedException();
        }

        static async Task<Image> DownloadImageAsync(string blobReference)
        {
            var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
            using (var memStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                return Image.FromStream(memStream);
            }
        }

        public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(Func<T1, T2, T3, R> func)
           => (T1 a) => (T2 b) => (T3 c) => func(a, b, c);

        public static Func<T1, Func<T2, Func<T3, Func<T4, R>>>> Curry<T1, T2, T3, T4, R>(
          Func<T1, T2, T3, T4, R> func)
         => (T1 a) => (T2 b) => (T3 c) => (T4 d) => func(a, b, c, d);


        public async Task<Image> BlendImagesFromBlobStorage(string blobReferenceOne, string blobReferenceTwo, Size size)
        {
            Func<Image, Func<Image, Func<Size, Image>>> BlendImagesCurried = Curry<Image, Image, Size, Image>(BlendImages);

            Task<Image> imageBlended =
                TaskEx.Pure(BlendImagesCurried)
                    .Apply(DownloadImageAsync(blobReferenceOne))
                    .Apply(DownloadImageAsync(blobReferenceTwo))
                    .Apply(TaskEx.Pure(size));
            return await imageBlended;
        }


        static async Task<Image> CreateThumbnail(string blobReference, int maxPixels)
        {
            Func<Image, Func<int, Image>> ToThumbnailCurried = Curry<Image, int, Image>(ToThumbnail);

            Image thumbnail = await TaskEx.Pure(ToThumbnailCurried)
                .Apply(DownloadImageAsync(blobReference))
                .Apply(TaskEx.Pure(maxPixels));

            return thumbnail;
        }

        //Listing 10.4 The Option type for error handling in a functional style
        static async Task<Option<Image>> DownloadOptionImage(string blobReference)  // #A
        {
            try
            {
                var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Some(Image.FromStream(memStream));    // #B
                }
            }
            catch (Exception)
            {
                return None;     // #B
            }
        }

        //Listing 10.7 AsyncOption cannot preserve the error details
        async Task<Option<Image>> DownloadOptionImage2(string blobReference)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("<Azure Connection>");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("Media");
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Some(Image.FromStream(memStream));
                }
            }
            catch (StorageException)
            {
                return None; // #A
            }
            catch (Exception)
            {
                return None;  // #A
            }
        }

        //Listing 10.9 DownloadResultImage to handle errors preserving the semantic
        async Task<Result<Image>> DownloadResultImages(string blobReference)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("<Azure Connection>");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("Media");
                await container.CreateIfNotExistsAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Image.FromStream(memStream);  // #A
                }
            }
            catch (StorageException exn)
            {
                return exn;   // #A
            }
            catch (Exception exn)
            {
                return exn;  // #A
            }
        }

        //Listing 10.11 Composing Task<Result<T>> operations in functional style
        async Task<Result<byte[]>> ProcessImage(string nameImage, string destinationImage)
        {
            return await DownloadResultImages(nameImage)
                   .Map(image => ToThumbnail(image))     // #A
                   .Bind(image => ToByteArray(image));    // #A
        }



        static void Main(string[] args)
        {
            Option<Image> bugghina2 = DownloadOptionImage("Bugghina002.jpg").Result;
        }


        Task<Result<byte[]>> ToByteArray(Image image)
        {
            return ResultExtensions.TryCatch(() =>
            {
                using (var memStream = new MemoryStream())
                {
                    image.Save(memStream, image.RawFormat);
                    return memStream.ToArray();
                }
            });
        }

        private static Task<Image> ToThumbnail(Image image)
        {
            return Task.Run(() =>
            {
                var bitmap = image.Clone() as Bitmap;
                var maxPixels = 400.0;
                var scaling =
                    bitmap.Width > bitmap.Height
                    ? maxPixels / Convert.ToDouble(bitmap.Width)
                    : maxPixels / Convert.ToDouble(bitmap.Height);
                var x = Convert.ToInt32(Convert.ToDouble(bitmap.Width) * scaling);
                var y = Convert.ToInt32(Convert.ToDouble(bitmap.Height) * scaling);
                return new Bitmap(bitmap.GetThumbnailImage(x, y, null, IntPtr.Zero)) as Image;
            });
        }


        static Image ToThumbnail(Image bitmap, int maxPixels)
        {
            var scaling = (bitmap.Width > bitmap.Height)
                          ? maxPixels / Convert.ToDouble(bitmap.Width)
                          : maxPixels / Convert.ToDouble(bitmap.Height);
            var width = Convert.ToInt32(Convert.ToDouble(bitmap.Width) * scaling);
            var heiht = Convert.ToInt32(Convert.ToDouble(bitmap.Height) * scaling);
            return new Bitmap(bitmap.GetThumbnailImage(width, heiht, null, IntPtr.Zero));
        }


        // Listing 10.20 Better composition of asynchronous operation using Applicative Functors
        static Func<T1, Func<T2, TR>> Curry<T1, T2, TR>(Func<T1, T2, TR> func) => p1 => p2 => func(p1, p2);

        static async Task<Image> CreateThumbnailCurry(string blobReference, int maxPixels)
        {
            Func<Image, Func<int, Image>> ToThumbnailCurried = Curry<Image, int, Image>(ToThumbnail); //#A

            Image thumbnail = await TaskEx.Pure(ToThumbnailCurried)     //#B
                            .Apply(DownloadImageAsync(blobReference))   //#C
                            .Apply(TaskEx.Pure(maxPixels));             //#C

            return thumbnail;
        }


        //Listing 10.21 Parallelize chain of computation with Applicative Functors
        static Image BlendImages(Image imageOne, Image imageTwo, Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height);
            using (var graphic = Graphics.FromImage(bitmap))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.DrawImage(imageOne,
                      new Rectangle(0, 0, size.Width, size.Height),
                      new Rectangle(0, 0, imageOne.Width, imageTwo.Height),
                      GraphicsUnit.Pixel);
                graphic.DrawImage(imageTwo,
                      new Rectangle(0, 0, size.Width, size.Height),
                      new Rectangle(0, 0, imageTwo.Width, imageTwo.Height),
                      GraphicsUnit.Pixel);
                graphic.Save();
            }
            return bitmap;
        }

        async Task<Image> BlendImagesFromBlobStorageAsync(string blobReferenceOne, string blobReferenceTwo, Size size)
        {
            Func<Image, Func<Image, Func<Size, Image>>> BlendImagesCurried =
                                    Curry<Image, Image, Size, Image>(BlendImages);
            Task<Image> imageBlended =
                    TaskEx.Pure(BlendImagesCurried)
                        .Apply(DownloadImageAsync(blobReferenceOne))
                        .Apply(DownloadImageAsync(blobReferenceTwo))
                        .Apply(TaskEx.Pure(size));
            return await imageBlended;
        }
    }

    class Log
    {
        internal static Task Error(Exception ex)
        {
            throw new NotImplementedException();
        }

        internal static void Error(string v, Exception ex)
        {
        }
    }
}