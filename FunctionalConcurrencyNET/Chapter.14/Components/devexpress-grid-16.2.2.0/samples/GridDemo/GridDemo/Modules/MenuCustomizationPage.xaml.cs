using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;
using DevExpress.Mobile.Core;

namespace DevExpress.GridDemo {
	public partial class MenuCustomizationPage : ContentPage {
		int menuRowHandle = GridControl.InvalidRowHandle;

		public MenuCustomizationPage() {
			InitializeComponent();

			BindData();
		}
		void OnPopupMenuCustomization(object sender, DevExpress.Mobile.DataGrid.PopupMenuEventArgs e) {
			switch(e.MenuType) {
				case GridPopupMenuType.DataRow:
					e.Menu.Items.Clear();
					PopupMenuItem item = new PopupMenuItem();
					item.Caption = "Send Email";
					item.Click += ItemClick;
					menuRowHandle = e.RowHandle;
					e.Menu.Items.Insert(0, item);
					break;

				case GridPopupMenuType.Header:
					e.Menu.Items.RemoveRange(2, 4);
					break;

				case GridPopupMenuType.TotalSummary:
					e.Menu.Items.RemoveAt(3);
					e.Menu.Items.RemoveAt(1);
					e.Menu.Items.RemoveAt(0);
					break;

				default:
					break;
			}
		}
		void ItemClick(object sender, EventArgs e) {
			if(menuRowHandle == GridControl.InvalidRowHandle)
				return;
			
			IRowData rowData = grid.GetRow(menuRowHandle);
			Customer selectedCustomer = rowData.DataObject as Customer;
			menuRowHandle = GridControl.InvalidRowHandle;
			try {
				Device.OpenUri(new Uri("mailto:" + selectedCustomer.Email));
			} catch {
			}
		}
		async void BindData() {
			BindingContext = await LoadData();
		}
		Task<MainPageViewModel> LoadData() {
			return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
		}
	}
}

