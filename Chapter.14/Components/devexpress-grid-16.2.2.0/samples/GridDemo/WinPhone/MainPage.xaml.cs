using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace GridDemo.WinPhone {
    public partial class MainPage : global::Xamarin.Forms.Platform.WinPhone.FormsApplicationPage {
        public MainPage() {
            InitializeComponent();
            SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;

            DevExpress.Mobile.Forms.Init();
            global::Xamarin.Forms.Forms.Init();
            var app = new DevExpress.GridDemo.App();
            app.MainPage.Appearing += OnMainPageAppearing;
            LoadApplication(app);
        }

        void OnMainPageAppearing(object sender, EventArgs e) {
            if (ApplicationBar.Buttons.Count == 0)
                return;

            ApplicationBarIconButton button = (ApplicationBar.Buttons[0] as ApplicationBarIconButton);
            
            if (button == null)
                return;

            button.Text = "Demos list";
            button.IconUri = new Uri(@"/ApplicationBar.DemosList.png", UriKind.Relative);
        }
    }
}
