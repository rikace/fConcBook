using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using System.Threading.Tasks;

namespace StockTicker.Client.iOS
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main(string[] args)
		{
            TaskScheduler.UnobservedTaskException += UnobservedTaskException;
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            DevExpress.Mobile.Forms.Init();
            UIApplication.Main(args, null, "AppDelegate");
		}

        private static void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // TODO
        }
    }
}
