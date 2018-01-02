using System;
using DevExpress.Mobile.DataGrid.Theme;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class HtmlViewerPage {
		new public static readonly BindableProperty ContentProperty = BindableProperty.Create("Source", typeof(WebViewSource), typeof(HtmlViewerPage), default(WebViewSource));

        public HtmlViewerPage() {
            InitializeComponent();
            this.BindingContext = this;
        }

        public WebViewSource Source {
            get { return (WebViewSource)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public Func<WebViewSource> ObtainSourceDelegate { get; set; }

        protected override void OnAppearing() {
            if (Source == null && ObtainSourceDelegate != null) {
                Source = ObtainSourceDelegate();
            }
            base.OnAppearing();
        }
    }
}
