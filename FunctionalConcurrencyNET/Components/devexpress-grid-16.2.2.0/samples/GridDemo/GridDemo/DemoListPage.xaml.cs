using System.Collections.Generic;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class DemoListPage {
        IList<DemoGroup> demoList;

        public DemoListPage() {
            InitializeComponent();
            this.BindingContext = this;
        }

        public ListView ListView { get { return listView; } }

        public IList<DemoGroup> DemoList {
            get { return demoList; }
            set {
                if (demoList == value)
                    return;
                this.demoList = value;
                UpdateItemSource();
            }
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            UpdateItemSource();
        }

        void UpdateItemSource() {
            if (listView != null) {
                listView.ItemsSource = this.demoList;
                if (listView.SelectedItem == null && demoList.Count > 0)
                    listView.SelectedItem = demoList[0];
            }
        }
    }

    public class DemoListCell : ViewCell {
        protected override void OnBindingContextChanged() {
            base.OnBindingContextChanged();
            if (Device.OS == TargetPlatform.iOS) {
                DemoInfo info = BindingContext as DemoInfo;
                if (info != null) {
					if(!string.IsNullOrEmpty(info.ShortDescription)) {
						Height = 80;
					}
                }
            }
        }
    }

    public class DemoListCellView : Grid {}
	public class DemoListView : ListView {}
    public class HeaderLabel : Label {}
}
