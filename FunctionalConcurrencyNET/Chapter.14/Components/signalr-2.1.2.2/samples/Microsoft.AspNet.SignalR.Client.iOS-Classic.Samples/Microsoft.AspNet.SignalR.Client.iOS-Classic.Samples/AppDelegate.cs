using System;
using System.Drawing;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Samples;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Microsoft.AspNet.SignalR.Client.iOS.Samples
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		const string SIGNALR_DEMO_SERVER = "http://YOUR-SERVER-INSTANCE-HERE";
		
		// class-level declarations
		UIWindow window;
		UINavigationController navController;

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			window = new UIWindow(UIScreen.MainScreen.Bounds);

			var controller = new UIViewController();
			var view = new UIView (UIScreen.MainScreen.Bounds);
			view.BackgroundColor = UIColor.White;
			controller.View = view;

			controller.NavigationItem.Title = "SignalR Client";

			var textView = new UITextView(new RectangleF(0, 0, 320, view.Frame.Height - 0));
			view.AddSubview (textView);


			navController = new UINavigationController (controller);

			window.RootViewController = navController;
			window.MakeKeyAndVisible();

			if (SIGNALR_DEMO_SERVER == "http://YOUR-SERVER-INSTANCE-HERE") {
				textView.Text = "You need to configure the app to point to your own SignalR Demo service.  Please see the Getting Started Guide for more information!";
				return true;
			}
			
			var traceWriter = new TextViewWriter(SynchronizationContext.Current, textView);

			var client = new CommonClient(traceWriter);
			client.RunAsync(SIGNALR_DEMO_SERVER);

			return true;
		}
	}
}
