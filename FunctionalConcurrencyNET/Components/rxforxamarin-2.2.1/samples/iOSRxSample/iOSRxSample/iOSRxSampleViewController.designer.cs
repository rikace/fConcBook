// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using System.CodeDom.Compiler;

#if __UNIFIED__
using Foundation;
using UIKit;
# else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace iOSRxSample
{
	[Register ("iOSRxSampleViewController")]
	partial class iOSRxSampleViewController
	{
		[Outlet]
		#if __UNIFIED__
		UIKit.UILabel lblStatus { get; set; }
		#else
		MonoTouch.UIKit.UILabel lblStatus { get; set; }
		#endif

		[Outlet]
		#if __UNIFIED__
		UIKit.UIScrollView scrollPad { get; set; }
		#else
		MonoTouch.UIKit.UIScrollView scrollPad { get; set; }
		#endif

		void ReleaseDesignerOutlets ()
		{
			if (lblStatus != null) {
				lblStatus.Dispose ();
				lblStatus = null;
			}

			if (scrollPad != null) {
				scrollPad.Dispose ();
				scrollPad = null;
			}
		}
	}
}
