using UIKit;
using System;
using System.Collections.ObjectModel;
using CoreGraphics;
using DevExpress.GridDemo;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace GridDemo.iOS.Controls {
	public class CustomTableViewSource : UITableViewSource {
		readonly ObservableCollection<DemoGroup> data;
		readonly ListView list;

		CustomTableViewCell selectedCell;

		public CustomTableViewSource(ListView list) {
			this.list = list;
			this.data = list.ItemsSource as ObservableCollection<DemoGroup>;
		}

		#region implemented abstract members of UITableViewSource

		public override UITableViewCell GetCell(UITableView tableView, Foundation.NSIndexPath indexPath) {
			int section = indexPath.Section;
			int row = indexPath.Row;
			CustomTableViewCell cell = (CustomTableViewCell)tableView.DequeueReusableCell("demos");
			if (cell == null) {
				cell = new CustomTableViewCell(UITableViewCellStyle.Default, "demos");
				cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			}
			cell.TextLabel.Text = data[section][row].Title;
			return cell;
		}

		public override UIView GetViewForHeader (UITableView tableView, nint section)
		{
			UIView header = new UIView(new CGRect(0, 0, tableView.Frame.Width, 50));
			header.BackgroundColor = UIColor.FromRGB(239, 239, 244);
			UILabel label = new UILabel(new CGRect(15, 0, tableView.Frame.Width, 50));
			label.Text = data[(int)section].Title.ToUpper();
			label.TextColor = UIColor.FromRGB(155, 155, 165);
			label.TextAlignment = UITextAlignment.Left;
			header.AddSubview(label);
			return header;
		}

		public override nfloat GetHeightForHeader (UITableView tableView, nint section) {
			return 50.0f;
		}

		public override string TitleForHeader (UITableView tableView, nint section) {
			return data[(int)section].Title;
		}

		public override void RowSelected(UITableView tableView, Foundation.NSIndexPath indexPath) {
			int section = indexPath.Section;
			int row = indexPath.Row;
			this.list.SelectedItem = data[section][row];
			if (selectedCell != null)
				selectedCell.SetHighlighted(false, true);
			selectedCell = (CustomTableViewCell)tableView.CellAt(indexPath);
			selectedCell.SetHighlighted(true, true);
		}

		public override nint NumberOfSections(UITableView tableView) {
			return data.Count;
		}

		public override nint RowsInSection(UITableView tableview, nint section) {
			return data[(int)section].Count;
		}

		#endregion
	}

}

