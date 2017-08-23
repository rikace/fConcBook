using UIKit;
using System.Collections.ObjectModel;
using DevExpress.GridDemo;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using GridDemo.iOS.Controls;

[assembly: ExportRenderer(typeof(DemoListView), typeof(GridDemo.iOS.Renderer.MasterViewRenderer))]

namespace GridDemo.iOS.Renderer {
	public class MasterViewRenderer : ListViewRenderer {
		protected override void OnElementChanged(ElementChangedEventArgs<ListView> e) {
			base.OnElementChanged(e);
			if (Control != null) {
				Control.Source = new CustomTableViewSource(Element);
			} 
		}
	}
}