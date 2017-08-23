using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class RestrictionsPage {
        public RestrictionsPage() {
            InitializeComponent();

            BindData();
        }
        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
