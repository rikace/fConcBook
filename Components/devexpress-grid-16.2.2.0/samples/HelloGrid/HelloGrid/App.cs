using System;
using Xamarin.Forms;

namespace HelloGrid
{
	public class App : Application {
		public App() {
			this.MainPage = GetMainPage();
		}
		public static Page GetMainPage () { 
			return new MainPage ();
		}
	}
}