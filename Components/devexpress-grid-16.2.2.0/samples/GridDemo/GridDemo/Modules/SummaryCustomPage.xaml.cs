using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {
    public partial class SummaryCustomPage {
        public SummaryCustomPage() {
            InitializeComponent();

            BindData();
        }

        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }

        void OnCalculateCustomSummary(object sender, CustomSummaryEventArgs e) {
            if (e.FieldName == "Total" && e.IsTotalSummary) {
                if (e.SummaryProcess == CustomSummaryProcess.Start) {
                    e.TotalValue = 0.0;
                }
                else if (e.SummaryProcess == CustomSummaryProcess.Calculate) {
                    double value = Convert.ToDouble(e.FieldValue);
                    if (value > 100.0)
                        e.TotalValue = Convert.ToDouble(e.TotalValue) + value;
                }
            }
        }
    }
}
