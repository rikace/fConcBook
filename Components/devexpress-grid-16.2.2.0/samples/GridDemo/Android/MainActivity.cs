using Android.App;
using Android.Content.PM;
using Android.OS;

using Xamarin.Forms.Platform.Android;

namespace DevExpress.GridDemo.Android {
	[Activity(Theme="@style/DemoTheme", Label = "Grid Demo", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsApplicationActivity {
		protected override void OnCreate(Bundle bundle) {
			base.OnCreate(bundle);
            DevExpress.Mobile.Forms.Init();
			Xamarin.Forms.Forms.Init(this, bundle);
			LoadApplication(new App());
		}
	}
}