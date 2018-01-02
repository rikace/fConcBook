using System;

using Foundation;
using AppKit;
using System.Threading;

namespace SignalRMac
{
    public partial class MainWindowController : NSWindowController
    {
        const string SIGNALR_DEMO_SERVER = "http://YOUR-SERVER-INSTANCE-HERE";

        public MainWindowController (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        public MainWindowController (NSCoder coder) : base (coder)
        {
        }

        public MainWindowController () : base ("MainWindow")
        {
        }

        public override void AwakeFromNib ()
        {
            base.AwakeFromNib ();
        }

        public new MainWindow Window {
            get { return (MainWindow)base.Window; }
        }

        public override void WindowDidLoad ()
        {
            base.WindowDidLoad ();

            if (SIGNALR_DEMO_SERVER == "http://YOUR-SERVER-INSTANCE-HERE") {
                textLog.Value = "You need to configure the app to point to your own SignalR Demo service.  Please see the Getting Started Guide for more information!";
                return;
            }

            var traceWriter = new TextViewWriter (SynchronizationContext.Current, textLog);

            var client = new CommonClient(traceWriter);
            client.RunAsync(SIGNALR_DEMO_SERVER);
        }
    }
}
