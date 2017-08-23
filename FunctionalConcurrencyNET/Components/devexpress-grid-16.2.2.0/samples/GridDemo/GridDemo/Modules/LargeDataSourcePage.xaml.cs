using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class LargeDataSourcePage {
        public LargeDataSourcePage() {
            InitializeComponent();

            activityIndicator.IsRunning = true;
            activityIndicator.IsVisible = true;
            loadingLabel.IsVisible = true;

            BindData();
        }

        async void BindData() {
            BindingContext = await LoadData();
            activityIndicator.IsRunning = false;
            activityIndicator.IsVisible = false;
            loadingLabel.IsVisible = false;
        }
        void OrderChange(int currentPercent) {
            loadingLabel.Text = string.Format("Loading data... {0}%", currentPercent);
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository(100000, OrderChange)));
        }
    }
}
