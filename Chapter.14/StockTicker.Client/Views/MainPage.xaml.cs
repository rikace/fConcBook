using DevExpress.Mobile.DataGrid;
using DevExpress.Mobile.DataGrid.Theme;
using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace StockTicker.Client
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

            viewModel = new MainPageViewModel(this);
            BindingContext = viewModel;

            ConfigureTheme();
        }

        private MainPageViewModel viewModel;

        private void ConfigureTheme()
        {
            ThemeManager.ThemeName = Themes.Light;

            // Header customization.
            ThemeManager.Theme.HeaderCustomizer.BackgroundColor = Color.FromRgb(30, 150, 170);
            ThemeManager.Theme.HeaderCustomizer.Font = new ThemeFontAttributes("Verdana",
                                        ThemeFontAttributes.FontSizeFromNamedSize(NamedSize.Medium),
                                        FontAttributes.None, Color.White);
            // Cell customization.
            ThemeManager.Theme.CellCustomizer.Font = new ThemeFontAttributes("Verdana",
                                        ThemeFontAttributes.FontSizeFromNamedSize(NamedSize.Medium),
                                        FontAttributes.None, Color.Black);

            ThemeManager.RefreshTheme();
        }

        private void stockGrid_SelectionChanged(object sender, RowEventArgs e)
        {
            var stock = viewModel.Stocks[e.RowHandle];
            viewModel.Symbol = stock.Symbol;
            viewModel.Price = stock.Price;
        }
    }
}
