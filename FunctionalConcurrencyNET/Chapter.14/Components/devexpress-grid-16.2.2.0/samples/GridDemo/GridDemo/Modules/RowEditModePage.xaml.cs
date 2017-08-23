using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {
    public partial class RowEditModePage {
        public RowEditModePage() {
            InitializeComponent();
			cbRowEditMode.Items.Add(RowEditMode.Inplace.ToString());
            cbRowEditMode.Items.Add(RowEditMode.Popup.ToString());
            cbRowEditMode.Items.Add(RowEditMode.GridArea.ToString());
            cbRowEditMode.Items.Add(RowEditMode.ScreenArea.ToString());
            cbRowEditMode.SelectedIndex = 0;

            BindData();
        }

        void OnRowEditModeChanged(object sender, EventArgs e) {
            grid.RowEditMode = GetRowEditMode();
        }
        RowEditMode GetRowEditMode() {
            return (RowEditMode)Enum.Parse(typeof(RowEditMode), cbRowEditMode.Items[cbRowEditMode.SelectedIndex]);
        }

        void OnInitNewRow(object sender, InitNewRowEventArgs e) {
            MainPageViewModel model = (MainPageViewModel)BindingContext;
            e.EditableRowData.SetFieldValue("Customer", model.Customers[0]);
            e.EditableRowData.SetFieldValue("Date", DateTime.Today);
        }

        async void BindData() {
            MainPageViewModel model = await LoadData();
            BindingContext = model;
            colCustomer.ItemsSource = model.Customers;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
