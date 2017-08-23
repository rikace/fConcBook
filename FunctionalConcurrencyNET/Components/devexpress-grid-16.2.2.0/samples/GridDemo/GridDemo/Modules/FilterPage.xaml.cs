using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.GridDemo {
    public partial class FilterPage {
        public FilterPage() {
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
