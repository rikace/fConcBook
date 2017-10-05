using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;

namespace PhotoViewer
{
    public partial class MainWindow : Window
    {
        public readonly string ImageFolderDestination = @"TempImageFolder";
        private FileSystemWatcher fileSystemWatcher;
        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();
        private CancellationToken cts;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateTimeElapsed(long timeElapsed)
        {
            Dispatcher.Invoke((Action)(() =>
              this.Title = string.Format($"Image Downloader - {timeElapsed} ms")));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Images = new ObservableCollection<BitmapImage>();
            cts = ctSource.Token;
            CleanUp();
            fileSystemWatcher = new FileSystemWatcher(ImageFolderDestination);
            fileSystemWatcher.Filter = "*.jpg";
            fileSystemWatcher.Created += new FileSystemEventHandler(FileChanged);
            fileSystemWatcher.EnableRaisingEvents = true;

            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(
                TaskUnobservedException_Handler);
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

        /// <summary>
        /// We fire off tasks but only process the first one that returns, and cancel the others.
        /// When we cancel, exceptions are thrown --- these exceptions need to be "observed"
        /// otherwise we get exceptions during garbage collection of the Task objects.  We avoid
        /// this problem via the following handler.
        ///
        /// NOTE: this is registered in the class's static constructor, so only registered once.
        /// </summary>
        private static void TaskUnobservedException_Handler(object sender, UnobservedTaskExceptionEventArgs e)
            => e.SetObserved();


        /// <summary>
        /// C# callable method to check internet access
        /// </summary>
        public static bool IsConnectedToInternet()
        {
            int Description;
            return InternetGetConnectedState(out Description, 0);
        }


        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public ObservableCollection<BitmapImage> Images { get; private set; }


        void FileChanged(object sender, FileSystemEventArgs e)
        {
            string imagePath = System.IO.Path.Combine(Environment.CurrentDirectory, ImageFolderDestination, e.Name);
            if (File.Exists(imagePath))
            {

                dispatcher.Invoke((Action)(async () =>
                {
                    bool done = false;
                    while (!done)
                    {
                        try
                        {
                            var bmp = new BitmapImage(new Uri(imagePath));
                            imageList.Items.Add(bmp);
                            done = true;
                        }
                        catch
                        {
                            // ignored
                            await Task.Delay(50, cts);
                        }
                    }
                }));
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            }
        }


        private void CleanUp()
        {
            this.Title = "Image Downloader";
            imageList.Items.Clear();
            if (!Directory.Exists(ImageFolderDestination))
                Directory.CreateDirectory(ImageFolderDestination);

            var files = Directory.GetFiles(ImageFolderDestination, "*.jpg");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            ctSource.Cancel();
            CleanUp();
        }
    }
}