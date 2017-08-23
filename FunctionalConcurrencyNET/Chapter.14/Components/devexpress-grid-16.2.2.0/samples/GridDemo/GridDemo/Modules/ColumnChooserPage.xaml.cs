using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.GridDemo {
    public partial class ColumnChooserPage {
        public ColumnChooserPage() {
            InitializeComponent();

            BindData();

        }

        void OnShowColumnChooser(object sender, EventArgs e) {
            grid.ShowColumnChooser();
        }

        async void BindData() {
            BindingContext = await LoadData();
            grid.ShowColumnChooser();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }

    }
}
