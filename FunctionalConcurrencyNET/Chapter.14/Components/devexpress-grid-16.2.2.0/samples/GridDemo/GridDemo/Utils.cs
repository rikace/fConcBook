using System;
using System.Globalization;
using DevExpress.Mobile.Core;
using DevExpress.Mobile.DataGrid.Theme;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class VisibilityStateToBoolValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            VisibilityState state = (VisibilityState)value;
            return state != VisibilityState.Never;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? VisibilityState.Always : VisibilityState.Never;
        }
    }

    public class IsSelectedToCellTextColorValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if(value is bool && (bool)value)
                return ThemeManager.Theme.CellCustomizer.SelectionFontColor;
            else
                return ThemeManager.Theme.CellCustomizer.Font.Color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}