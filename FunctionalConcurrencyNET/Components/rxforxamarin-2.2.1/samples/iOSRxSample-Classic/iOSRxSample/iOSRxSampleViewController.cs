using System;
using System.Drawing;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive;

#if __UNIFIED__
using Foundation;
using UIKit;
# else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace iOSRxSample
{
	public partial class iOSRxSampleViewController : UIViewController
	{
		public iOSRxSampleViewController () : base ("iOSRxSampleViewController", null)
		{
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// Set the Image inside the ImageView
			var imgView = new UIImageView (UIImage.FromBundle ("leaf.jpg")) {
				ContentMode = UIViewContentMode.Center
			};

			// Set the scrollpad content size so it has plenty of room to scroll (Image Size)
			scrollPad.ContentSize = imgView.Bounds.Size;
			scrollPad.AddSubview (imgView);

			// Subscribe to normal event handler so when ScrollView is Decelerating
			// this will be called
			scrollPad.DraggingEnded += (object sender, DraggingEventArgs e) => {
				if (e.Decelerate == true) {
					Console.WriteLine ("Dragging ended, Decelerate:{0}", e.Decelerate);
					InvokeOnMainThread (() => lblStatus.Text = "Try Again.");
				}
			};

			// The Rx fun starts here, we will look at DraggingEnded event and look inside its DraggingEventArgs
			// to see if Decelerate == false, so only then we will "React" to the event.
			ScrollReactSource = Observable.FromEventPattern<DraggingEventArgs> (scrollPad, "DraggingEnded")
				.Where (ev => ev.EventArgs.Decelerate == false)
				.ToEventPattern ();

			ReactOnDecelerate += (sender, ev) => 
				InvokeOnMainThread (() => {                
					lblStatus.Text = "Cool you did it!! Rx Working!";
					Console.WriteLine ("Dragging ended from Rx, Decelerate:false");
				});

		}

		IEventPatternSource<DraggingEventArgs> ScrollReactSource;

		public event EventHandler<DraggingEventArgs> ReactOnDecelerate {
			add { ScrollReactSource.OnNext += value; }
			remove { ScrollReactSource.OnNext -= value; }
		}
	}
}