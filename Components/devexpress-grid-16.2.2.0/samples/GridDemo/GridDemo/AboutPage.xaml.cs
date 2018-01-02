using System;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class AboutPage {
        public AboutPage() {
            InitializeComponent();
            this.BindingContext = this;
        }

        public string Name { get { return "DevExpress Grid for Xamarin Forms"; } }
        public string Description { get { return "The FREE and feature-rich DevExpress Grid for Xamarin ships with dozens of high-impact Microsoft Outlook-inspired capabilities for your next iOS, Android & Windows Phone app."; } }
        public string Version { get { return "Version " + DevExpress.Internal.AssemblyInfo.Version; } }
        public string Copyright { get { return DevExpress.Internal.AssemblyInfo.AssemblyCopyright; } }
    }
}