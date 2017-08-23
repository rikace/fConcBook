using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System;
using DevExpress.Mobile.DataGrid.Theme;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace StockTicker.Client
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

            MainPage = new MainPage();
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
