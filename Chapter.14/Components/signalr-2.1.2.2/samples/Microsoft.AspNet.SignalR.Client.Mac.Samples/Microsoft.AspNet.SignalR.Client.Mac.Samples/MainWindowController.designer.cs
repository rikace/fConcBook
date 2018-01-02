// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace SignalRMac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSTextView textLog { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (textLog != null) {
				textLog.Dispose ();
				textLog = null;
			}
		}
	}
}
