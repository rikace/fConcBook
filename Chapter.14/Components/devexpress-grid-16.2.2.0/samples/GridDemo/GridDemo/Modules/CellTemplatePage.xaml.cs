using System.Threading.Tasks;

namespace DevExpress.GridDemo {
    public partial class CellTemplatePage {
        public CellTemplatePage() {
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