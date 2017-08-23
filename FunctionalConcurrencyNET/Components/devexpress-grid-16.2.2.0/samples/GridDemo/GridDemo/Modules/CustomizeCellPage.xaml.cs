using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class CustomizeCellPage {
        public CustomizeCellPage() {
            InitializeComponent();

            BindData();
        }
        void OnCustomizeCell(CustomizeCellEventArgs e) {
            if (e.FieldName == "Total" && !e.IsSelected) {
                decimal total = Convert.ToDecimal(e.Value);
                if (total < 100)
                    e.BackgroundColor = Color.Red;
                else if (total > 500)
                    e.BackgroundColor = Color.Green;
                e.Handled = true;
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
