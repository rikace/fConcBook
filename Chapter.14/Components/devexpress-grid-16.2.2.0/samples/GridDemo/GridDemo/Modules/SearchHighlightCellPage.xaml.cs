using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;
using Xamarin.Forms;
using DevExpress.Mobile.DataGrid.Theme;
using System.Globalization;

namespace DevExpress.GridDemo {
    public partial class SearchHighlightCellPage {
        #region fields
        string searchText;
        #endregion

        public SearchHighlightCellPage() {
            InitializeComponent();

            BindData();
            searchText = string.Empty;
        }
        
        void OnCustomizeCell(CustomizeCellEventArgs e) {
            if (string.IsNullOrEmpty(searchText))
                return;
            string cellText = e.DisplayText.ToUpper();
            if (cellText.Contains(searchText.ToUpper()) && !e.IsSelected) {
                e.BackgroundColor = ThemeManager.Theme.CellCustomizer.HighlightColor;
                e.ForeColor = Color.Black;
                e.Handled = true;
            }
        }
        void OnSearchTextChanged(object sender, EventArgs args) {
            SearchBar searchBar = (SearchBar)sender;
            searchText = searchBar.Text;
            grid.Redraw(false);
        }
        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
