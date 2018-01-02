using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid.Theme;

namespace DevExpress.GridDemo {
    public partial class ThemesPage {
        public ThemesPage() {
            InitializeComponent();

            BindData();
        }
        void OnDarkTheme(object sender, EventArgs e) {
            ThemeManager.ThemeName = Themes.Dark;
        }
        void OnLightTheme(object sender, EventArgs e) {
            ThemeManager.ThemeName = Themes.Light;
        }
        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
