using System;
using System.Collections.Generic;
using Xamarin.Forms;
using DevExpress.Mobile.DataGrid;

namespace HelloGrid
{	
	public partial class MainPage : ContentPage
	{	
		int count;
		public MainPage ()
		{
			InitializeComponent ();

			TestOrdersRepository model = new TestOrdersRepository ();
			BindingContext = model;
		}

		void OnCalculateCustomSummary(object sender, CustomSummaryEventArgs e){
			if (e.FieldName.ToString () == "Shipped")
			if (e.IsTotalSummary){
				if (e.SummaryProcess == CustomSummaryProcess.Start) {
					count = 0;
				}
				if (e.SummaryProcess == CustomSummaryProcess.Calculate) {
					if (!(bool)e.FieldValue)
						count++;
					e.TotalValue = count;
				}
			}
		}
	}
}

