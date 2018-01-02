using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {	
	public partial class SwipeButtonsPage {	
		public SwipeButtonsPage () {
			InitializeComponent ();

			BindData();
		}

		async void BindData() {
			BindingContext = await LoadData();
		}

		Task<MainPageViewModel> LoadData() {
			return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
		}

		void OnSwipeButtonShowing(object sender, SwipeButtonShowingEventArgs e) {
			if(e.ButtonInfo.ButtonName == "RemoveButton") {
				if((e.SourceRowIndex < 0) || ((e.RowHandle % 2) == 0)) {
					e.IsVisible = false;
				}
			}
		}

		void OnSwipeButtonClick(object sender, SwipeButtonEventArgs e) {
			if(e.ButtonInfo.ButtonName != "RemoveButton") {
				DisplayAlert("Alert from " + e.ButtonInfo.ButtonName, "The " + e.ButtonInfo.ButtonName + " was clicked", "Ok");
			}
		}
	}
}

