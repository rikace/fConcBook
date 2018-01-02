using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //string sourcePath = e.Args.Length > 0 ? e.Args.Last() : throw new ArgumentException("Folder for temp images cannot be empty");
            //if (!Directory.Exists(sourcePath)) Directory.CreateDirectory(sourcePath);
        }
    }
}
