using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Samples;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Microsoft.AspNet.SignalR.Client.Android.Samples
{
	[Activity (Label = "Microsoft.AspNet.SignalR.Client.Android.Samples", MainLauncher = true)]
	public class MainActivity : Activity
	{
		const string SIGNALR_DEMO_SERVER = "http://YOUR-SERVER-INSTANCE-HERE";

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);
			var textView = FindViewById<TextView>(Resource.Id.textView);
			
			if (SIGNALR_DEMO_SERVER == "http://YOUR-SERVER-INSTANCE-HERE") {
				textView.Text = "You need to configure the app to point to your own SignalR Demo service.  Please see the Getting Started Guide for more information!";
				return;
			}
			
			var traceWriter = new TextViewWriter(SynchronizationContext.Current, textView);

			var client = new CommonClient(traceWriter);
			client.RunAsync(SIGNALR_DEMO_SERVER);
		}
	}
}
