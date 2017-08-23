using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.DataGrid.Localization;
using DevExpress.Mobile.DataGrid.Theme;
using Xamarin.Forms;
using DevExpress.Utils.Localization;
using DevExpress.Utils.Localization.Internal;
using System.Resources;
using Xamarin.Forms.Xaml;
using DevExpress.Mobile.DataGrid;

namespace DevExpress.GridDemo {
    public partial class LocalizationPage {
        public LocalizationPage() {
            InitializeComponent();

            GridLocalizer.SetResource("DevExpress.GridDemo.Localization.GridLocalizationRes", typeof(LocalizationPage).GetTypeInfo().Assembly);
            UpdateCurrentCultureName();
            BindData();
        }
        void OnDefaultCulture(object sender, EventArgs e) {
            SetCulture(null);
        }
        void OnEnglishCulture(object sender, EventArgs e) {
            SetCulture(new CultureInfo("en-US"));
        }
        void OnFrenchCulture(object sender, EventArgs e) {
            SetCulture(new CultureInfo("fr-FR"));
        }
        void OnGermanCulture(object sender, EventArgs e) {
            SetCulture(new CultureInfo("de-DE"));
        }
        void OnSpanishCulture(object sender, EventArgs e) {
            SetCulture(new CultureInfo("es-ES"));
        }
        void OnRussianCulture(object sender, EventArgs e) {
            SetCulture(new CultureInfo("ru-RU"));
        }

        void SetCulture(CultureInfo culture) {
            IGlobalizationService service = GlobalServices.Instance.GetService<IGlobalizationService>();
            if (service == null)
                return;

            if (culture == null)
                culture = service.CurrentOSCulture;

            service.CurrentCulture = culture;
            service.CurrentUICulture = culture;

            GridLocalizer.ResetCache();
            DemoLocalizer.ResetCache();

            DemoStringIdLocalizer localizer = this.Resources["localizer"] as DemoStringIdLocalizer;
            if (localizer != null)
                localizer.CultureName = culture.Name;

            UpdateCurrentCultureName();

            grid.Redraw(true);
        }
        void UpdateCurrentCultureName() {
            IGlobalizationService service = GlobalServices.Instance.GetService<IGlobalizationService>();
            if (service == null) {
                currentCultureName.Text = "unknown";
                return;
            }
            currentCultureName.Text = service.CurrentCulture.Name;
        }
        async void BindData() {
            BindingContext = await LoadData();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
        void OnInitNewRow(object sender, InitNewRowEventArgs e) {
            MainPageViewModel model = (MainPageViewModel)BindingContext;
            e.EditableRowData.SetFieldValue("Customer", model.Customers[0]);
            e.EditableRowData.SetFieldValue("Date", DateTime.Today);
        }
    }

    public enum DemoStringId {
        Caption_ColumnName,
        Caption_ColumnDate,
        Caption_ColumnTotal,
    }

    #region DemoLocalizer
    public class DemoLocalizer : XtraLocalizer<DemoStringId> {
        static DemoLocalizer() {
            SetActiveLocalizerProvider(new GlobalActiveLocalizerProvider<DemoStringId>(CreateDefaultLocalizer()));
        }

        #region PopulateStringTable
        protected override void PopulateStringTable() {
            AddString(DemoStringId.Caption_ColumnName, "Name");
            AddString(DemoStringId.Caption_ColumnDate, "Date");
            AddString(DemoStringId.Caption_ColumnTotal, "Total");
        }
        #endregion

        public override XtraLocalizer<DemoStringId> CreateResXLocalizer() {
            return new DemoResLocalizer();
        }
        public static XtraLocalizer<DemoStringId> CreateDefaultLocalizer() {
            return new DemoResLocalizer();
        }
        public static string GetString(DemoStringId id) {
            return Active.GetLocalizedString(id);
        }
        public static void ResetCache() {
            Active.Reset();
        }
    }
    #endregion
    #region DemoResLocalizer
    public class DemoResLocalizer : XtraResXLocalizer<DemoStringId> {
        public DemoResLocalizer()
            : base(new DemoLocalizer()) {
        }

        protected override ResourceManager CreateResourceManagerCore() {
            return new ResourceManager("DevExpress.GridDemo.Localization.LocalizationPageRes", typeof(DemoResLocalizer).GetTypeInfo().Assembly);
        }
    }
    #endregion

    public class DemoStringIdLocalizer : StringIdConverter<DemoStringId> {
        static DemoStringIdLocalizer() {
            // initialize localizer
            DemoLocalizer.GetString(DemoStringId.Caption_ColumnName);
        }
        protected override XtraLocalizer<DemoStringId> Localizer { get { return DemoLocalizer.Active; } }
    }
}
