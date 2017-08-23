using System;
using System.IO;
using System.Threading.Tasks;
using DevExpress.Export;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.IO;
using Xamarin.Forms;
using System.Reflection;

namespace DevExpress.GridDemo {
    public partial class SaveLoadStatePage {
        public SaveLoadStatePage() {
            InitializeComponent();
            cbStateList.Items.Add("Default");
            cbStateList.Items.Add("Compact");
            cbStateList.Items.Add("Grouped");
            cbStateList.SelectedIndex = 0;
            //cbStateList.SelectedIndexChanged += cbStateList_SelectedIndexChanged;
            BindData();
        }

        void OnStateIndexChanged(object sender, EventArgs e) {
            //throw new NotImplementedException();
            LoadState(cbStateList.Items[cbStateList.SelectedIndex]);
        }
        async void BindData() {
            MainPageViewModel model = await LoadData();
            BindingContext = model;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }

        string customState = String.Empty;
        void OnSave(object sender, EventArgs e) {
            customState = grid.SaveLayoutToXml();
            if (!cbStateList.Items.Contains("Custom"))
                cbStateList.Items.Add("Custom");
        }
        void LoadState(string name) {
            if (name == "Custom" && !String.IsNullOrEmpty(customState))
                grid.RestoreLayoutFromXml(customState);
            else {
                string resourceName = "DevExpress.GridDemo." + name.ToLower() + "_state.xml";
                Stream stream = GetType().GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
                grid.RestoreLayoutFromStream(stream);
            }
        }
    }
}
