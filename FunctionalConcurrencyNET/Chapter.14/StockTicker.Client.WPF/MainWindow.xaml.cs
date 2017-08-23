using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace StockTicker.Client.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            viewModel = new MainWindowViewModel(this);
            this.DataContext = viewModel;

            //this.stockGrid.DataContext = viewModel.Stocks;
        }

        private MainWindowViewModel viewModel;

        internal void DisplayAlert(string v1, string v2, string v3)
        {
            MessageBox.Show(v2, v1, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private Regex regex = new Regex("[^0-9.,]+");
        private void NumberOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = regex.IsMatch(e.Text);
        }

        private void stockGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stock = e.AddedItems[0] as Model.StockModelObject;
            viewModel.Symbol = stock.Symbol;
            viewModel.Price = stock.Price;
        }

    }
}
