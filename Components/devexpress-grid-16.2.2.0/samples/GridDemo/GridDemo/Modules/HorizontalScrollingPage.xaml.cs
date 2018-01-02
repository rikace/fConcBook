using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Threading.Tasks;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {
	public partial class HorizontalScrollingPage {
		GridColumn ActiveColumn { get; set; }

		public HorizontalScrollingPage() {
			InitializeComponent();
			BindData();

			ActiveColumn = null;
			horizontalScrollingCheckEdit.CheckedChanged += (object sender, EventArgs e) => {
				grid.ColumnsAutoWidth = !horizontalScrollingCheckEdit.IsChecked.Value;
			};
		}
		async void BindData() {
			MainPageViewModel model = await LoadData();
			BindingContext = model;
		}
		Task<MainPageViewModel> LoadData() {
			return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
		}
		void OnPopupMenuCustomization(object sender, PopupMenuEventArgs e) {
			if((e.MenuType != GridPopupMenuType.Header) || grid.ColumnsAutoWidth)
				return;

			ActiveColumn = e.Column;

			if ((e.Column.FixedStyle == FixedStyle.Right) || (e.Column.FixedStyle == FixedStyle.None)) {
				CreatePopupMenuItem(e, "Fix Column to Left", OnLeftFixedColumnItemClick);
			}

			if ((e.Column.FixedStyle == FixedStyle.Left) || (e.Column.FixedStyle == FixedStyle.None)) {
				CreatePopupMenuItem(e, "Fix Column to Right", OnRightFixedColumnItemClick);
			}

			if ((e.Column.FixedStyle == FixedStyle.Left) || (e.Column.FixedStyle == FixedStyle.Right)) {
				CreatePopupMenuItem(e, "Unfix Column", OnNoneFixedColumnItemClick);
			}
		}

		void CreatePopupMenuItem(PopupMenuEventArgs e, string caption, EventHandler eventHandler) {
			PopupMenuItem item = new PopupMenuItem();
			item.Caption = caption;
			item.Click += eventHandler;
			e.Menu.Items.Add(item);
		}

		void OnLeftFixedColumnItemClick(object sender, EventArgs e) {
			OnFixedItemClickCore(FixedStyle.Left, sender as PopupMenuItem, OnLeftFixedColumnItemClick);
		}

		void OnRightFixedColumnItemClick(object sender, EventArgs e) {
			OnFixedItemClickCore(FixedStyle.Right, sender as PopupMenuItem, OnRightFixedColumnItemClick);
		}

		void OnNoneFixedColumnItemClick(object sender, EventArgs e) {
			OnFixedItemClickCore(FixedStyle.None, sender as PopupMenuItem, OnNoneFixedColumnItemClick);
		}

		void OnFixedItemClickCore(FixedStyle style, PopupMenuItem item, EventHandler eventHandler) {
			item.Click -= eventHandler;

			if(ActiveColumn == null)
				return;

			ActiveColumn.FixedStyle = style;
			ActiveColumn = null;
		}
	}
}

