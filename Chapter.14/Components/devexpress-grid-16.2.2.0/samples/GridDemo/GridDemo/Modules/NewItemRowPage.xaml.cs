using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {
    public partial class NewItemRowPage {
        public NewItemRowPage() {
            InitializeComponent();

            BindData();
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
