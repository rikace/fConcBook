using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.DataGrid.Localization;
using Xamarin.Forms;
using System.ComponentModel;
using DevExpress.Mobile.DataGrid.Theme;

namespace DevExpress.GridDemo {
    public partial class RealTimeDataPage : ISupportsStartFinish {
        MainPageViewModel model;
        const string TriangleNegativeFileName = "Triangle_Negative.png";
        const string TrianglePositiveFileName = "Triangle_Positive.png";

        public RealTimeDataPage() {
            InitializeComponent();

            SetCulture(new CultureInfo("en-US"));
            BindData();
        }

        void SetCulture(CultureInfo culture) {
            IGlobalizationService service = GlobalServices.Instance.GetService<IGlobalizationService>();
            if(service == null)
                return;

            if(culture == null)
                culture = service.CurrentOSCulture;

            service.CurrentCulture = culture;
            service.CurrentUICulture = culture;

            GridLocalizer.ResetCache();
            grid.Redraw(true);
        }
        async void BindData() {
            this.model = await LoadData();
            BindingContext = model;
        }
        protected override void OnBindingContextChanged() {
            base.OnBindingContextChanged();
            ISupportsStartFinish startFinish = this;
            startFinish.Start();
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }

        void ISupportsStartFinish.Start() {
            if(model == null)
                return;

            model.StartMarketSimulation();
        }

        void ISupportsStartFinish.Finish() {
            if(model == null)
                return;

            this.model.StopMarketSimulation();
            this.model = null;
        }

        Label lastChangedLabel;
        double labelPreviousValue;

        void OnLabelPropertyChanging(object sender, PropertyChangingEventArgs e) {
            if(!(sender is Label))
                return;

            if(e.PropertyName == "Text")
                OnLabelTextChanging(sender as Label);
        }

        void OnLabelPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(!(sender is Label))
                return;

            if(e.PropertyName == "Text")
                OnLabelTextChanged(sender as Label);
            else if(e.PropertyName == "Renderer")
                ApplyThemeToLabel(sender as Label);
        }

        void OnLabelTextChanging(Label label) {
            double value = GetLabelValue(label);
            if(!Double.IsNaN(value)) {
                lastChangedLabel = label;
                labelPreviousValue = value;
            }
        }

        void OnLabelTextChanged(Label label) {
            if(label == lastChangedLabel) {
                double currentValue = GetLabelValue(label);
                if(!Double.IsNaN(currentValue)) {
                    if(currentValue != labelPreviousValue) {
                        Image image = GetLabelImageSibling(label);
                        if(image != null)
                            image.Source = currentValue > labelPreviousValue ? TrianglePositiveFileName : TriangleNegativeFileName;
                        label.TextColor = currentValue > labelPreviousValue ? Color.Green : Color.Red;
                    }
                }
            }
        }

        double GetLabelValue(Label label) {
            double value;
            if (!String.IsNullOrEmpty(label.Text) && (Double.TryParse(label.Text, out value) || Double.TryParse(label.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out value)))
                return value;
            else
                return Double.NaN;
        }

        Image GetLabelImageSibling(Label label) {
            if(label.Parent is Grid)
                foreach(View gridChild in (label.Parent as Grid).Children)
                    if(gridChild is Image)
                        return gridChild as Image;
            return null;
        }

        void ApplyThemeToLabel(Label label) {
            if(label.FontSize != ThemeManager.Theme.CellCustomizer.Font.Size)
                label.FontSize = ThemeManager.Theme.CellCustomizer.Font.Size;
        }
    }
}
