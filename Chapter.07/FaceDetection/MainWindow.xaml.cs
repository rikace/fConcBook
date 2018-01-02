using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using Functional.Tasks;
using Pipeline;
using Microsoft.FSharp.Core;

namespace FaceDetection
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ImagesList.ItemsSource = Images;
        }

        private const string ImagesFolder = "Images";

        public ObservableCollection<BitmapImage> Images = new ObservableCollection<BitmapImage>();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var fileName in Directory.GetFiles(ImagesFolder))
            {
                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    Images.Add(stream.BitmapImageFromStream());
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Images.Clear();
            //StartFaceDetection_7_6(ImagesFolder);
            //StartFaceDetection_7_7(ImagesFolder);
            //StartFaceDetection_7_13(ImagesFolder);
            StartFaceDetection_Pipeline_FSharpFunc(ImagesFolder);
        }

        //Listing 7.5 Face Detection function in C#
        Bitmap DetectFaces_7_5(string fileName)
        {
            var imageFrame = new Image<Bgr, byte>(fileName);  // #A
            var cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml"); // #B
            var grayframe = imageFrame.Convert<Gray, byte>(); // #A

            var faces = cascadeClassifier.DetectMultiScale(
              grayframe, 1.1, 3, System.Drawing.Size.Empty); // #C
            foreach (var face in faces)
                imageFrame.Draw(face, new Bgr(System.Drawing.Color.DarkRed), 3); // #D

            return imageFrame.ToBitmap();
        }

        void StartFaceDetection(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);
            foreach (string filePath in filePaths)
            {
                var bitmap = DetectFaces_7_5(filePath);
                var bitmapImage = bitmap.ToBitmapImage();
                Images.Add(bitmapImage); //#E
            }
        }

        // Listing 7.6  Task Parallel implementation of the detect faces program
        void StartFaceDetection_7_6(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);

            var bitmaps = from filePath in filePaths
                          select Task.Run<Bitmap>(() => DetectFaces_7_5(filePath)); // #A

            foreach (var bitmap in bitmaps)
            {
                var bitmapImage = bitmap.Result;
                Images.Add(bitmapImage.ToBitmapImage());
            }
        }


        // Listing 7.7 Correct Task Parallel implantation of the Detect Faces function
        ThreadLocal<CascadeClassifier> CascadeClassifierThreadLocal =
            new ThreadLocal<CascadeClassifier>(() => new CascadeClassifier("haarcascade_frontalface_alt_tree.xml")); // #A

        Bitmap DetectFaces_7_7(string fileName)
        {
            var imageFrame = new Image<Bgr, byte>(fileName);
            var cascadeClassifier = CascadeClassifierThreadLocal.Value;
            var grayframe = imageFrame.Convert<Gray, byte>();

            var faces = cascadeClassifier.DetectMultiScale(
                grayframe, 1.1, 3, System.Drawing.Size.Empty);
            foreach (var face in faces)
                imageFrame.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
            return imageFrame.ToBitmap();
        }

        void StartFaceDetection_7_7(string imagesFolder)
        {
            var filePaths = Directory.GetFiles(imagesFolder);
            var bitmapTasks =
                (from filePath in filePaths
                 select Task.Run<Bitmap>(() => DetectFaces_7_7(filePath))).ToList(); // #B

            foreach (var bitmapTask in bitmapTasks)
                bitmapTask.ContinueWith(bitmap =>   // #C
                {
                    var bitmapImage = bitmap.Result;
                    Images.Add(bitmapImage.ToBitmapImage());
                }, TaskScheduler.FromCurrentSynchronizationContext()); // #D
        }


        // Listing 7.8 DetectFaces function using Task-Continuation
        Task<Bitmap> DetectFaces_7_8(string fileName)
        {
            var imageTask = Task.Run<Image<Bgr, byte>>(
                () => new Image<Bgr, byte>(fileName)
            );
            var imageFrameTask = imageTask.ContinueWith(
                image => image.Result.Convert<Gray, byte>()
            ); // #A
            var grayframeTask = imageFrameTask.ContinueWith(
               imageFrame => imageFrame.Result.Convert<Gray, byte>()
            ); // #A

            var facesTask = grayframeTask.ContinueWith(grayFrame =>
                {
                    var cascadeClassifier = CascadeClassifierThreadLocal.Value;
                    return cascadeClassifier.DetectMultiScale(
                        grayFrame.Result, 1.1, 3, System.Drawing.Size.Empty);
                }
            ); // #A

            var bitmapTask = facesTask.ContinueWith(faces =>
                {
                    foreach (var face in faces.Result)
                        imageTask.Result.Draw(
                              face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return imageTask.Result.ToBitmap();
                }
            ); // #A
            return bitmapTask;
        }

        // Listing 7.10 Detect Faces function using Task-Continuation based on LINQ Expression
        Task<Bitmap> DetectFaces_7_10(string fileName)
        {
            Func<System.Drawing.Rectangle[], Image<Bgr, byte>, Bitmap> drawBoundries = (faces, image) =>
            {
                faces.ForAll(face => image.Draw(face, new
                    Bgr(System.Drawing.Color.BurlyWood), 3)); // #B
                return image.ToBitmap();
            };

            return from image in Task.Run(() => new Image<Bgr, byte>(fileName))
                   from imageFrame in Task.Run(() => image.Convert<Gray, byte>())
                   from bitmap in Task.Run(() => CascadeClassifierThreadLocal.Value.DetectMultiScale(
                                                imageFrame, 1.1, 3, System.Drawing.Size.Empty)
                                  ).Select(faces => drawBoundries(faces, image))
                   select bitmap; // #A
        }

        void StartFaceDetection_7_13(string imagesFolder)
        {
            // Listing 7.13 The refactor Detect-Face code using the parallel Pipeline

            var files = Directory.GetFiles(ImagesFolder);

            Func<string, Image<Bgr, byte>> imageFn =
                (fileName) => new Image<Bgr, byte>(fileName);
            Func<Image<Bgr, byte>, Tuple<Image<Bgr, byte>, Image<Gray, byte>>> grayFn =
                image => Tuple.Create(image, image.Convert<Gray, byte>());
            Func<Tuple<Image<Bgr, byte>, Image<Gray, byte>>,
                 Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                 CascadeClassifierThreadLocal.Value.DetectMultiScale(
                     frames.Item2, 1.1, 3, System.Drawing.Size.Empty));
            Func<Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>, Bitmap> drawFn =
                faces =>
                {
                    foreach (var face in faces.Item2)
                        faces.Item1.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return faces.Item1.ToBitmap();
                };

            var imagePipe =
                Pipeline.PipelineFunc.Pipeline<string, Image<Bgr, byte>>
                    .Create(imageFn)
                    .Then(grayFn)
                    .Then(detectFn)
                    .Then(drawFn); // #A

            CancellationTokenSource cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token);  // #B

            foreach (string fileName in files)
                imagePipe.Enqueue(fileName,
                    ((tup) =>
                    {
                        Application.Current.Dispatcher.Invoke(
                            () => Images.Add(tup.Item2.ToBitmapImage()));
                        return (Unit)Activator.CreateInstance(typeof(Unit), true);
                    })); // #C
        }

        void StartFaceDetection_Pipeline_FSharpFunc(string imagesFolder)
        {
            var files = Directory.GetFiles(ImagesFolder);

            Func<string, Image<Bgr, byte>> imageFn =
                (fileName) => new Image<Bgr, byte>(fileName);
            Func<Image<Bgr, byte>, Tuple<Image<Bgr, byte>, Image<Gray, byte>>> grayFn =
                image => Tuple.Create(image, image.Convert<Gray, byte>());
            Func<Tuple<Image<Bgr, byte>, Image<Gray, byte>>,
                 Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>> detectFn =
                frames => Tuple.Create(frames.Item1,
                 CascadeClassifierThreadLocal.Value.DetectMultiScale(
                     frames.Item2, 1.1, 3, System.Drawing.Size.Empty));
            Func<Tuple<Image<Bgr, byte>, System.Drawing.Rectangle[]>, Bitmap> drawFn =
                faces =>
                {
                    foreach (var face in faces.Item2)
                        faces.Item1.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3);
                    return faces.Item1.ToBitmap();
                };

            var imagePipe =
                Pipeline<string, Image<Bgr, byte>>
                    .Create(imageFn.ToFSharpFunc())
                    .Then(grayFn.ToFSharpFunc())
                    .Then(detectFn.ToFSharpFunc())
                    .Then(drawFn.ToFSharpFunc()); // #A

            CancellationTokenSource cts = new CancellationTokenSource();
            imagePipe.Execute(4, cts.Token);  // #B

            foreach (string fileName in files)
                imagePipe.Enqueue(fileName,
                    FSharpFuncUtils.Create<Tuple<string, Bitmap>, Unit>
                    ((tup) =>
                    {
                        Application.Current.Dispatcher.Invoke(
                            () => Images.Add(tup.Item2.ToBitmapImage()));
                        return (Unit)Activator.CreateInstance(typeof(Unit), true);
                    })); // #C
        }
    }

    internal static class Helpers
    {
        public static BitmapImage BitmapImageFromStream(this Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Position = 0;
                return BitmapImageFromStream(ms);
            }
        }

        public static T[] ForAll<T>(this T[] array, Action<T> action)
        {
            foreach (var item in array)
                action(item);
            return array;
        }
    }
}
