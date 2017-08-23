using System;
using System.IO;
using System.Threading.Tasks;
using DevExpress.Export;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.IO;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class ExportPage {
        public ExportPage() {
            InitializeComponent();
            cbExportType.Items.Add(ExportTarget.Xlsx.ToString());
            cbExportType.Items.Add(ExportTarget.Xls.ToString());
            cbExportType.Items.Add(ExportTarget.Csv.ToString());
            cbExportType.SelectedIndex = 0;
            BindData();
        }
        async void BindData() {
            MainPageViewModel model = await LoadData();
            BindingContext = model;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
        ExportTarget GetCurrentExportType() {
            return (ExportTarget)Enum.Parse(typeof(ExportTarget), cbExportType.Items[cbExportType.SelectedIndex]);
        }
        void OnOpen(object sender, EventArgs e) {
            if(String.IsNullOrEmpty(txtPath.Text))
                return;
            IShellService shell = GlobalServices.Instance.GetService<IShellService>();
            if(shell != null)
                shell.OpenFile(txtPath.Text, btnOpen);
            else
                DisplayAlert("GridDemo", "Unable to open a file. The Shell service is not found.", "Cancel");
        }
        void OnExport(object sender, EventArgs e) {
            ExportTarget exportType = GetCurrentExportType();
            string fileName = "grid." + exportType.ToString().ToLower();
            IPathService pathServise = GlobalServices.Instance.GetService<IPathService>();
            if(pathServise == null)
                return;
            
            string filePath = pathServise.GetAbsolutePath(fileName);
            pathServise.EnsurePathExists(filePath);

            using(FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                if(grid.ExportToExcel(stream, exportType))
                    txtPath.Text = stream.AbsolutePath;
                else {
                    txtPath.Text = String.Empty;
                    DisplayAlert("GridDemo", "Make sure you have an application for opening " + exportType.ToString() + " files.", "Cancel");
                }
            }
        }
    }
}
