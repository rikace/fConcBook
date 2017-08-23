using DevExpress.Data.Filtering;
using DevExpress.Mobile.DataGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using DevExpress.Mobile.DataGrid.Theme;

namespace DevExpress.GridDemo {
    public partial class TestPage : ContentPage {
        class CustomerNameComparer : IComparer<IRowData> {
            #region IComparer implementation
            public int Compare(IRowData x, IRowData y) {
                return Comparer<string>.Default.Compare((string)x.GetFieldValue("Customer.Name"), (string)y.GetFieldValue("Customer.Name"));
            }
            #endregion
        }

        public TestPage() {
            InitializeComponent();
            //List<OrderSeverity> severityList = new List<OrderSeverity>();
            //severityList.Add(new OrderSeverity() { Severity = Severity.Severe, DisplayText = "SEVERE !" });
            //severityList.Add(new OrderSeverity() { Severity = Severity.Moderate, DisplayText = "Moderate" });
            //severityList.Add(new OrderSeverity() { Severity = Severity.Minor, DisplayText = "minor" });
            //colSeverity.ItemsSource = severityList;
            //colSeverity.DisplayMember = "DisplayText";
            //colSeverity.ValueMember = "Severity";
            BindData();

			grid.SwipeButtonShowing += Showing;
			grid.SwipeButtonClick += Click;
			//grid.Theme = new DevExpress.Mobile.DataGrid.Theme.DarkTheme();
            SetAppearanceForControlPanel();
			//

 			//grid.Scroller.SetBinding<MainPageViewModel>(GridControl.IsRefreshingProperty, v => v.IsBusy);
            //grid.Filter.ColumnFilters.Add(new GridAutoFilter() { FilterExpression = CriteriaOperator.Parse("[Customer.Name] LIKE 'A%'") });
            
            /*
            grid.dataController.Comparer = new CustomerNameComparer();
            grid.dataController.GetRow(0);
            grid.dataController.Comparer = null;
            grid.dataController.GetRow(0);
            grid.dataController.Comparer = new CustomerNameComparer();
            grid.dataController.GetRow(0);
            */
            //grid.BackgroundColor = Color.Gray;
        }

		void Showing(object sender, SwipeButtonShowingEventArgs e) {
			if(e.RowHandle == 1) {
				if(e.ButtonInfo.ButtonName == "Test1") {
					e.IsVisible = false;
				}
			}
		}

		void Click(object sender, SwipeButtonEventArgs e) {

		}

        void gridCustomizeCell(CustomizeCellEventArgs e) {
            if (e.FieldName == "Total" && !e.IsSelected) {
                string name = (string)grid.GetCellValue(e.RowHandle, "Customer.Name");
                if (!String.IsNullOrEmpty(name) && name.StartsWith("ann", StringComparison.CurrentCultureIgnoreCase)) {
                    decimal total = Convert.ToDecimal(e.Value);
                    if (total < 100)
                        e.BackgroundColor = Color.Red;
                    else if (total > 1000)
                        e.BackgroundColor = Color.Green;
                    e.Handled = true;
                }
            }
        }
        void gridOnCustomUnboundColumnData(object sender, GridColumnDataEventArgs e) {
            if (e.Column.FieldName == "Number3") {
                if (e.IsGetData)
                    e.Value = (int)e.RowData.GetFieldValue("Id") * 3;
                else if (e.IsSetData)
                    e.EditableRowData.SetFieldValue("Id", -371);
            }
        }
        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
			return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
        void OnGroupClicked(object sender, EventArgs e) {
            Debug.WriteLine("Click");
            grid.Columns["Customer.Name"].IsGrouped = btnGroup.Text == "Group By Name" ? true : false;
            //if (btnGroup.Text == "Group By Name")
            //    grid.BeginRowEdit(grid.SelectedRowHandle, "Id");
            //else
            //    grid.EndRowEdit(grid.SelectedRowHandle);
            btnGroup.Text = btnGroup.Text == "Group By Name" ? "Remove Grouping" : "Group By Name";
            //grid.DeleteRow(grid.SelectedRowHandle);
        }
        void OnChangeThemeClicked(object sender, EventArgs e) {
			GridThemeManager.ThemeName = GridThemeManager.ThemeName == Themes.Default ? Themes.Dark : Themes.Default;
            SetAppearanceForControlPanel();
            btnChangeTheme.Text = btnChangeTheme.Text == "Dark Theme" ? "Light Theme" : "Dark Theme";
            //string ourText = btnGroup.Text;
            //btnGroup.Text = "Changing...";
            //btnGroup.Text = ourText;
        }
        void OnScrollClicked(object sender, EventArgs e) {
            grid.ScrollToRow(50);
        }
        void SetAppearanceForControlPanel() {
			if (GridThemeManager.ThemeName == Themes.Dark) {
                SetBlackButtons();
            } else {       
                SetWhiteButtons();
            }
        }

        void SetWhiteButtons() {
            controlPanel.BackgroundColor = Color.FromRgb(150, 150, 150);
            btnChangeTheme.ButtonColor = Color.FromRgb(210, 210, 210);
            btnChangeTheme.TextColor = Color.Black;
            btnChangeTheme.ImageSource = "lightChart.png";

            btnGroup.BorderColor = Color.FromRgb(150, 150, 150);
            btnGroup.ButtonColor = Color.FromRgb(210, 210, 210);
            btnGroup.TextColor = Color.Black;
			btnGroup.ImageSource = "lightFolder.png";
        }

        void SetBlackButtons() {
            controlPanel.BackgroundColor = GridThemeManager.Theme.CellCustomizer.BorderColor;
            btnChangeTheme.ButtonColor = Color.Black;
            btnChangeTheme.TextColor = Color.White;
			btnChangeTheme.ImageSource = "darkChart.png";

            btnGroup.BorderColor = GridThemeManager.Theme.CellCustomizer.BorderColor;
            btnGroup.ButtonColor = Color.Black;
            btnGroup.TextColor = Color.White;
			btnGroup.ImageSource = "darkFolder.png";
        }
    }
}