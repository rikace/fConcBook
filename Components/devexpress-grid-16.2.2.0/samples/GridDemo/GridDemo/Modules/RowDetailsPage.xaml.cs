using DevExpress.Mobile.DataGrid;
using System.Threading.Tasks;

namespace DevExpress.GridDemo {
    public partial class RowDetailsPage {
        public RowDetailsPage() {
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
