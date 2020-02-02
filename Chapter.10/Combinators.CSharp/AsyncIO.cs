using System;
using System.IO;
using System.Threading.Tasks;
using Functional.CSharp.Concurrency.Async;
using Functional.CSharp.FuctionalType;
using Microsoft.WindowsAzure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SixLabors.Primitives;

namespace Combinators.CSharp
{
    using static OptionHelpers;

    public static class AsyncIO
    {
        //Listing 10.1 DownloadImage with traditional imperative error handling
        public static async Task<Image> DownloadImage(string blobReference)
        {
            try
            {
                var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false); // #A
                var blockBlob = container.GetBlockBlobReference(blobReference); // #A
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false); // #A
                    return Image.Load<Rgba32>(memStream);
                }
            }
            catch (StorageException ex)
            {
                Logging.Error("Azure Storage error", ex); // #B
                throw;
            }
            catch (Exception ex)
            {
                Logging.Error("Some general error", ex); // #B
                throw;
            }
        }

        public static async Task RunDownloadImage() // #C
        {
            try
            {
                var image = await DownloadImage("Bugghina0001.jpg");
                ProcessImage(image);
            }
            catch (Exception ex)
            {
                HandlingError(ex); // #B
                throw;
            }
        }

        public static async Task RunDownloadImageWithRetry() // #C
        {
            Image image = await AsyncEx.Retry(async () =>
                    await DownloadImageAsync("Bugghina001.jpg")
                        .Otherwise(async () =>
                            await DownloadImageAsync("Bugghina002.jpg")),
                5, TimeSpan.FromSeconds(2));

            ProcessImage(image);
        }

        public static async Task<Image<Rgba32>> DownloadImageAsync(string blobReference)
        {
            var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false);
            var blockBlob = container.GetBlockBlobReference(blobReference);
            using (var memStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                return Image.Load<Rgba32>(memStream);
            }
        }


        //Listing 10.4 The Option type for error handling in a functional style
        public static async Task<Option<Image<Rgba32>>> DownloadOptionImage(string blobReference) // #A
        {
            try
            {
                var container = await Helpers.GetCloudBlobContainerAsync().ConfigureAwait(false);
                var blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Some(Image.Load<Rgba32>(memStream)); // #B
                }
            }
            catch (Exception)
            {
                return None; // #B
            }
        }

        //Listing 10.7 AsyncOption cannot preserve the error details
        public static async Task<Option<Image<Rgba32>>> DownloadOptionImage2(string blobReference)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse("<Azure Connection>");
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("Media");
                await container.CreateIfNotExistsAsync();

                var blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Some(Image.Load<Rgba32>(memStream));
                }
            }
            catch (StorageException)
            {
                return None; // #A
            }
            catch (Exception)
            {
                return None; // #A
            }
        }

        //Listing 10.9 DownloadResultImage to handle errors preserving the semantic
        public static async Task<Result<Image<Rgba32>>> DownloadResultImage(string blobReference)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse("<Azure Connection>");
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("Media");
                await container.CreateIfNotExistsAsync();

                var blockBlob = container.GetBlockBlobReference(blobReference);
                using (var memStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return Image.Load<Rgba32>(memStream); // #A
                }
            }
            catch (StorageException exn)
            {
                return exn; // #A
            }
            catch (Exception exn)
            {
                return exn; // #A
            }
        }

        public static void HandlingError(Exception ex)
        {
            throw new NotImplementedException();
        }

        public static void ProcessImage(Image image)
        {
            throw new NotImplementedException();
        }


        public static Task<Result<byte[]>> ToByteArray(Image image)
        {
            return ResultExtensions.TryCatch(() =>
            {
                using (var memStream = new MemoryStream())
                {
                    image.Save(memStream, new JpegEncoder());
                    return memStream.ToArray();
                }
            });
        }

        public static Task<Image<Rgba32>> ToThumbnail(Image<Rgba32> image)
        {
            return Task.Run(() =>
            {
                var bitmap = image.Clone();
                var maxPixels = 400.0;

                var scaling =
                    bitmap.Width > bitmap.Height
                        ? maxPixels / Convert.ToDouble(bitmap.Width)
                        : maxPixels / Convert.ToDouble(bitmap.Height);
                var x = Convert.ToInt32(Convert.ToDouble(bitmap.Width) * scaling);
                var y = Convert.ToInt32(Convert.ToDouble(bitmap.Height) * scaling);

                bitmap.Mutate(o => o.Resize(x, y, new BoxResampler()));
                return bitmap;
            });
        }

        public static Image<Rgba32> ToThumbnail(Image<Rgba32> bitmap, int maxPixels)
        {
            var scaling = bitmap.Width > bitmap.Height
                ? maxPixels / Convert.ToDouble(bitmap.Width)
                : maxPixels / Convert.ToDouble(bitmap.Height);
            var width = Convert.ToInt32(Convert.ToDouble(bitmap.Width) * scaling);
            var heiht = Convert.ToInt32(Convert.ToDouble(bitmap.Height) * scaling);

            bitmap.Mutate(o => o.Resize(width, heiht, new BoxResampler()));
            return bitmap;
        }

        // Listing 10.20 Better composition of asynchronous operation using Applicative Functors
        public static Func<T1, Func<T2, TR>> Curry<T1, T2, TR>(Func<T1, T2, TR> func)
        {
            return p1 => p2 => func(p1, p2);
        }

        public static Func<T1, Func<T2, Func<T3, TR>>> Curry<T1, T2, T3, TR>(Func<T1, T2, T3, TR> func)
        {
            return p1 => p2 => p3 => func(p1, p2, p3);
        }


        //Listing 10.21 Parallelize chain of computation with Applicative Functors
        public static Image<Rgba32> BlendImages(Image<Rgba32> imageOne, Image<Rgba32> imageTwo, Size size)
        {
            var bitmap = new Image<Rgba32>(size.Width, size.Height);

            var imageOneCopy = imageOne.Clone();
            var imageTwoCopy = imageTwo.Clone();
            imageOneCopy.Mutate(o => o.Resize(size));
            imageTwoCopy.Mutate(o => o.Resize(size));

            bitmap.Mutate(o =>
                o.DrawImage(imageOneCopy, new Point(0, 0), 1)
                    .DrawImage(imageTwoCopy, new Point(100, 0), 1));

            return bitmap;
        }
    }
}