using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DevExpress.Export;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.Core.ConditionalFormatting;
using DevExpress.Mobile.DataGrid;
using DevExpress.Mobile.IO;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class ConditionalFormattingPage {
        readonly Dictionary<string, FormatConditionBase> formats = new Dictionary<string, FormatConditionBase>();

        public ConditionalFormattingPage() {
            PopulateFormats();
            InitializeComponent();
            foreach (string name in formats.Keys)
                cbConditionalFormatting.Items.Add(name);
            cbConditionalFormatting.SelectedIndex = 0;
            BindData();
        }

        void OnConditionalFormattingSelectedIndexChanged(object sender, EventArgs e) {
            ApplyConditionalFormatting();
        }
        async void BindData() {
            MainPageViewModel model = await LoadData();
            BindingContext = model;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
        FormatConditionBase GetCurrentFormatCondition() {
            string name = cbConditionalFormatting.Items[cbConditionalFormatting.SelectedIndex];
            FormatConditionBase result;
            if (formats.TryGetValue(name, out result))
                return result;
            return null;
        }
        void ApplyConditionalFormatting() {
            grid.FormatConditions.Clear();
            FormatConditionBase condition = GetCurrentFormatCondition();
            if (condition != null)
                grid.FormatConditions.Add(condition);
        }

        void PopulateFormats() {
            AddDataBarFormatCondition();
            AddExpressionFormatCondition();
            AddColorScaleFormatCondition();
            AddTopBottomFormatCondition();
			AddIconSetFormatCondition();
            AddNoFormatCondition();
        }

        void AddExpressionFormatCondition() {
            FormatCondition condition = new FormatCondition();
            condition.FieldName = "Total";
            condition.Expression = "[Total] > 3000";
            condition.PredefinedFormatName = "BoldText";
            condition.ApplyToRow = true;
            formats.Add("Bold Totals > 3000", condition);
        }
        void AddColorScaleFormatCondition() {
            ColorScaleFormatCondition condition = new ColorScaleFormatCondition();
            condition.FieldName = "Total";
            condition.PredefinedFormatName = "RedYellowGreenColorScale";
            formats.Add("Totals Color Scale", condition);
        }
        void AddTopBottomFormatCondition() {
            TopBottomRuleFormatCondition condition = new TopBottomRuleFormatCondition();
            condition.FieldName = "Total";
            condition.Threshold = 5;
            condition.Rule = TopBottomRule.TopItems;
            condition.PredefinedFormatName = "YellowFillWithDarkYellowText";
            formats.Add("Top 5 Totals", condition);
        }
        void AddDataBarFormatCondition() {
            DataBarFormatCondition condition = new DataBarFormatCondition();
            condition.FieldName = "Total";
            condition.PredefinedFormatName = "LightBlueGradientDataBar";
            formats.Add("Totals Data Bars", condition);
        }
        void AddIconSetFormatCondition() {
            IconSetFormatCondition condition = new IconSetFormatCondition();
            condition.FieldName = "Total";
            condition.PredefinedFormatName = "Flags3IconSet";
            formats.Add("Flag Totals", condition);
        }
        void AddNoFormatCondition() {
            formats.Add("None", null);
        }
    }
}
