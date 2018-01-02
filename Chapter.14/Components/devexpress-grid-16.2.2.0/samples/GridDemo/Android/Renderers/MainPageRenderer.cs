using Xamarin.Forms.Platform.Android;
using Android.Views;
using Xamarin.Forms;

[assembly: ExportRenderer(typeof(DevExpress.GridDemo.MainPage), typeof(DevExpress.GridDemo.Android.MainPageRenderer))]
namespace DevExpress.GridDemo.Android {
    public class MainPageRenderer : MasterDetailRenderer {

        public override bool OnInterceptTouchEvent(MotionEvent ev) {
            bool result = base.OnInterceptTouchEvent(ev);
            if (Element == null)
                return result;

            if (Device.Idiom == TargetIdiom.Tablet && IsInLandscape())
                result = false;
            
            return result;
        }

        internal bool IsInLandscape() {
            return global::Android.App.Application.Context.Resources.Configuration.Orientation == global::Android.Content.Res.Orientation.Landscape;
        }
    }
}
