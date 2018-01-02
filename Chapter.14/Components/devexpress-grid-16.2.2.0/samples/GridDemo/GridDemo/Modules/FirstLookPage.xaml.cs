using System;
using System.IO;
using System.Threading.Tasks;
using DevExpress.Mobile.Core;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class FirstLookPage {
        public FirstLookPage() {
            InitializeComponent();
            BindData();
        }
        async void BindData() {
            MainPageViewModel model = await LoadData();
            BindingContext = model;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
