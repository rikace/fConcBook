using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DlibDotNet;
using Functional.CSharp;
using Functional.CSharp.Concurrency.Tasks;
using Microsoft.FSharp.Core;
using Pipeline.FSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceDetection
{
    public class FaceDetection
    {
        // Listing 7.7 Correct Task Parallel implantation of the Detect Faces function
        private readonly ThreadLocal<FrontalFaceDetector> FrontalFaceDetectorThreadLocal =
            new ThreadLocal<FrontalFaceDetector>(() => Dlib.GetFrontalFaceDetector()); // #A

        private Func<Array2D<RgbPixel>, Array2D<RgbPixel>> ConvertToGray => img =>
        {
            var grayImage = new Array2D<RgbPixel>(img.Rows, img.Columns);

            for (var x = 0; x < img.Rows; x++)
                for (var y = 0; y < img.Columns; y++)
                {
                    var pixel = img[x][y];
                    var gray = (byte)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);
                    grayImage[x][y] = new RgbPixel(gray, gray, gray);
                }
            
            return grayImage;
        };

        private Func<Array2D<RgbPixel>, Image<Rgba32>> ConvertToRgba32 => img =>
        {
            var image = new Image<Rgba32>(img.Rows, img.Columns);
            for (var x = 0; x < img.Rows; x++)
                for (var y = 0; y < img.Columns; y++)
                {
                    var pixel = img[x][y];
                    image[x, y] = new Rgba32(pixel.Red, pixel.Green, pixel.Blue);
                }

            return image;
        };

        private Func<Image, byte[]> ConvertToBytes => img =>
        {
            using (var stream = new MemoryStream())
            {
                img.SaveAsJpeg(stream);
                stream.Flush();
                return stream.ToArray();
            }
        };

        private Func<Image<Rgba32>, Array2D<RgbPixel>> ConvertToRgbPixel => img =>
        {
            var image = new Array2D<RgbPixel>(img.Width, img.Height);
            for (var x = 0; x < img.Width; x++)
                for (var y = 0; y < img.Height; y++)
                {
                    var pixel = img[x, y];
                    image[x][y] = new RgbPixel(pixel.R, pixel.G, pixel.B);
                }

            return image;
        };

        // Listing 7.5 Face Detection function in C#
        public Image<Rgba32> DetectFaces_7_5(string inputFilePath)
        {
            using (var fd = Dlib.GetFrontalFaceDetector())
            {
                var img = Dlib.LoadImage<RgbPixel>(inputFilePath); // #A
                // var cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml"); // #B

                var grayImage = ConvertToGray(img);

                // find all faces in the image
                var faces = fd.Operator(grayImage); // #C
                foreach (var face in faces)
                    // draw a rectangle for each face
                    Dlib.DrawRectangle(img, face, new RgbPixel(255, 0, 0), 6); // #D

                return Image.Load<Rgba32>(img.ToBytes(), new SixLabors.ImageSharp.Formats.Jpeg.JpegDecoder());
            }
        }

        public List<Image<Rgba32>> StartFaceDetection(string imagesFolder)
        {
            var images = new List<Image<Rgba32>>();
            var filePaths = Directory.GetFiles(imagesFolder);
            foreach (var filePath in filePaths)
            {
                var bitmap = DetectFaces_7_5(filePath);
                images.Add(bitmap); //#E
            }

            return images;
        }

        // Listing 7.6  Task Parallel implementation of the detect faces program
        public List<Image<Rgba32>> StartFaceDetection_7_6(string imagesFolder)
        {
            var images = new List<Image<Rgba32>>();
            var filePaths = Directory.GetFiles(imagesFolder);

            var bitmaps = from filePath in filePaths
                          select Task.Run(() => DetectFaces_7_5(filePath)); // #A

            foreach (var bitmap in bitmaps)
            {
                var bitmapImage = bitmap.Result;
                images.Add(bitmapImage);
            }

            return images;
        }

        public Image<Rgba32> DetectFaces_7_7(string inputFilePath)
        {
            var img = Dlib.LoadImage<RgbPixel>(inputFilePath); // #A
            var fd = FrontalFaceDetectorThreadLocal.Value; // #B
            var grayImage = ConvertToGray(img);

            // find all faces in the image
            var faces = fd.Operator(grayImage); // #C
            foreach (var face in faces) 
                Dlib.DrawRectangle(img, face, new RgbPixel(255, 0, 0), 6); // #D

            return Image.Load<Rgba32>(img.ToBytes(), new SixLabors.ImageSharp.Formats.Jpeg.JpegDecoder());
        }

        public List<Image<Rgba32>> StartFaceDetection_7_7(string imagesFolder)
        {
            var images = new List<Image<Rgba32>>();
            var filePaths = Directory.GetFiles(imagesFolder);
            var bitmapTasks =
                (from filePath in filePaths
                 select Task.Run(() => DetectFaces_7_7(filePath))).ToList(); // #B

            foreach (var bitmapTask in bitmapTasks)
                bitmapTask.ContinueWith(bitmap => // #C
                {
                    var bitmapImage = bitmap.Result;
                    images.Add(bitmapImage);
                }  /*, TaskScheduler.FromCurrentSynchronizationContext() */ ); // #D

            return images;
        }

        // Listing 7.8 DetectFaces function using Task-Continuation
        private Task<Image<Rgba32>> DetectFaces_7_8(string inputFilePath)
        {
            var imageTask = Task.Run(() => Image.Load(inputFilePath));
            var imageRgb32Task = imageTask.ContinueWith(image =>
                Image.LoadPixelData<Rgba32>(ConvertToBytes(image.Result), image.Result.Width, image.Result.Height));

            var imageFrameTask = imageRgb32Task.ContinueWith(
                image => ConvertToRgbPixel(image.Result)
            ); // #A

            var grayFrameTask = imageFrameTask.ContinueWith(
                imageFrame => ConvertToGray(imageFrame.Result)
            ); // #A

            var facesTask = grayFrameTask.ContinueWith(grayFrame =>
                {
                    var frontalFaceDetector = FrontalFaceDetectorThreadLocal.Value;
                    return frontalFaceDetector.Operator(grayFrame.Result);
                }
            ); // #A

            var bitmapTask = facesTask.ContinueWith(faces =>
                {
                    var image = imageFrameTask.Result;
                    foreach (var face in faces.Result)
                        Dlib.DrawRectangle(image, face, new RgbPixel(255, 0, 0),
                            6); // #D

                    return ConvertToRgba32(image);
                }
            ); // #A
            return bitmapTask;
        }

        // Listing 7.10 Detect Faces function using Task-Continuation based on LINQ Expression
        private Task<Image<Rgba32>> DetectFaces_7_10(string inputFilePath)
        {
            Func<Rectangle[], Array2D<RgbPixel>, Image<Rgba32>> drawBoundries = (faces, image) =>
            {
                faces.ForAll(face =>
                        Dlib.DrawRectangle(image, face, new RgbPixel(255, 0, 0),
                            6) // #B
                );
                return ConvertToRgba32(image);
            };

            return from image in Task.Run(() => Image.Load(inputFilePath))
                   from imageRgb32 in Task.Run(() =>
                       Image.LoadPixelData<Rgba32>(ConvertToBytes(image), image.Width, image.Height))
                   from imageFrame in Task.Run(() => ConvertToRgbPixel(imageRgb32))
                   from grayFrame in Task.Run(() => ConvertToGray(imageFrame))
                   from facesTask in Task.Run(() => FrontalFaceDetectorThreadLocal.Value.Operator(grayFrame))
                   select drawBoundries(facesTask, imageFrame);
        }

        public List<Image<Rgba32>> StartFaceDetection_7_13(string imagesFolder)
        {
            // Listing 7.13 The refactor Detect-Face code using the parallel Pipeline
            var files = Directory.GetFiles(imagesFolder);

            Func<string, Image<Rgba32>> imageFn = fileName => Image.Load<Rgba32>(fileName);
            Func<Image<Rgba32>, Array2D<RgbPixel>> rgbPixelFn = image => ConvertToRgbPixel(image);
            Func<Array2D<RgbPixel>, Tuple<Array2D<RgbPixel>, Array2D<RgbPixel>>> grayFn = image =>
                Tuple.Create(image, ConvertToGray(image));
            Func<Tuple<Array2D<RgbPixel>, Array2D<RgbPixel>>, Tuple<Array2D<RgbPixel>, Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                    FrontalFaceDetectorThreadLocal.Value.Operator(frames.Item2));

            Func<Tuple<Array2D<RgbPixel>, Rectangle[]>, Image<Rgba32>> drawFn =
                faces =>
                {
                    var image = faces.Item1;
                    foreach (var face in faces.Item2)
                        Dlib.DrawRectangle(image, face, new RgbPixel(255, 0, 0), 6);

                    return ConvertToRgba32(image);
                };


            var imagePipe =
                PipelineFunc.Pipeline<string, Image<Rgba32>>
                    .Create(imageFn)
                    .Then(rgbPixelFn)
                    .Then(grayFn)
                    .Then(detectFn)
                    .Then(drawFn); // #A

            var cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token); // #B

            var images = new List<Image<Rgba32>>();

            foreach (var fileName in files)
                imagePipe.Enqueue(fileName,
                    tup =>
                    {
                        images.Add(tup.Item2);
                        return (Unit)Activator.CreateInstance(typeof(Unit), true);
                    }); // #C

            return images;
        }

        public void StartFaceDetection_Pipeline_FSharpFunc(string imagesFolder)
        {
            var files = Directory.GetFiles(imagesFolder);

            Func<string, Image<Rgba32>> imageFn = fileName => Image.Load<Rgba32>(fileName);
            Func<Image<Rgba32>, Array2D<RgbPixel>> rgbPixelFn = image => ConvertToRgbPixel(image);
            Func<Array2D<RgbPixel>, Tuple<Array2D<RgbPixel>, Array2D<RgbPixel>>> grayFn = image =>
                Tuple.Create(image, ConvertToGray(image));
            Func<Tuple<Array2D<RgbPixel>, Array2D<RgbPixel>>, Tuple<Array2D<RgbPixel>, Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                    FrontalFaceDetectorThreadLocal.Value.Operator(frames.Item2));

            Func<Tuple<Array2D<RgbPixel>, Rectangle[]>, Image<Rgba32>> drawFn =
                faces =>
                {
                    var image = faces.Item1;
                    foreach (var face in faces.Item2)
                        Dlib.DrawRectangle(image, face, new RgbPixel(255, 0, 0), 6);

                    return ConvertToRgba32(image);
                };

            var imagePipe =
                Pipeline<string, Image<Rgba32>>
                    .Create(Interop.ToFSharpFunc(imageFn))
                    .Then(Interop.ToFSharpFunc(rgbPixelFn))
                    .Then(Interop.ToFSharpFunc(grayFn))
                    .Then(Interop.ToFSharpFunc(detectFn))
                    .Then(Interop.ToFSharpFunc(drawFn)); // #A

            var cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token); // #B

            var images = new List<Image<Rgba32>>();
            foreach (var fileName in files)
                imagePipe.Enqueue(fileName,
                    FSharpFuncUtils.Create<Tuple<string, Image<Rgba32>>, Unit>
                    (tup =>
                    {
                        images.Add(tup.Item2);
                        return (Unit)Activator.CreateInstance(typeof(Unit), true);
                    })); // #C
        }
    }
}