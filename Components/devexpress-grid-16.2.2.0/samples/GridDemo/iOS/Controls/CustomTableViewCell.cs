using UIKit;
using System.Collections.ObjectModel;
using DevExpress.GridDemo;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using GridDemo.iOS.Controls;

namespace GridDemo.iOS.Controls {
	public class CustomTableViewCell : UITableViewCell {
		public CustomTableViewCell(UITableViewCellStyle style, string id) : base(style, id) { }

		public override void SetHighlighted(bool highlighted, bool animated) {
			if (highlighted) { 
				this.TextLabel.TextColor = Color.White.ToUIColor();
				this.BackgroundColor = UIColor.FromRGB(76, 161, 255);
			} else {
				this.TextLabel.TextColor = Color.Black.ToUIColor();
				this.BackgroundColor = UIColor.White;
			}
		}
	}
}

