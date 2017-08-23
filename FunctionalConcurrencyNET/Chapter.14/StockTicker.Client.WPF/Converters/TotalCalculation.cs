using StockTicker.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace StockTicker.Client.WPF.Views.Converters
{
    class TotalCalculation : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                          object parameter, CultureInfo culture)
        {
            var order = (Models.OrderRecord)values[0];           
            return (order.Price * order.Quantity).ToString("N2");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}