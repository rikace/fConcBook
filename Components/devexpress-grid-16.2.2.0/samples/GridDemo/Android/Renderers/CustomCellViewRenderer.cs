using System.ComponentModel;
using Android.Graphics;
using Android.Widget;
using DevExpress.GridDemo;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Color = Xamarin.Forms.Color;

[assembly: ExportRenderer(typeof(DemoListCellView), typeof(GridDemoApp.Android.CustomCellViewRenderer))]
[assembly: ExportRenderer(typeof(HeaderLabel), typeof(GridDemoApp.Android.HeaderLabelRenderer))]

namespace GridDemoApp.Android {
    public class CustomCellViewRenderer : VisualElementRenderer<DemoListCellView> {
        protected override void OnElementChanged(ElementChangedEventArgs<DemoListCellView> e) {
            SetBackgroundResource(Resource.Drawable.list_selector_pressed);
            base.OnElementChanged(e);
        }
    }

    public class HeaderLabelRenderer : LabelRenderer {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e) {
            base.OnElementChanged(e);
            Element.Layout(new Rectangle(Element.X + 50, Element.Y, Element.Width, Element.Height));
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == "Renderer") {
                UpdateText();
            }
        }

        void UpdateText() {
            TextView nativeLabelView = (TextView)Control;

            nativeLabelView.TextSize = 14.0f;
            nativeLabelView.SetTypeface(null, TypefaceStyle.Bold);
            nativeLabelView.SetTextColor(Color.FromRgb(95, 92, 97).ToAndroid());
        }
    }
}