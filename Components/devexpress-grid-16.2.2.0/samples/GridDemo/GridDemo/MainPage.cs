using System.Collections.Generic;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class MainPage : MasterDetailPage {
        public MainPage() {
            InitializeComponent();
        }

        public IList<DemoGroup> DemoList {
            get { return ((DemoListPage)this.Master).DemoList; }
            set {
                ((DemoListPage)this.Master).DemoList = value;
                if (value != null) {
                    DemoListPage listPage = this.Master as DemoListPage;
                    if (listPage != null)
                        listPage.ListView.SelectedItem = DemoList[0][0];
                }
            }
        }

        void InitializeComponent() {
            DemoListPage listPage = new DemoListPage();
            DemoContentPage content = new DemoContentPage();
            this.Master = listPage;
            this.Detail = content;
            listPage.ListView.ItemSelected += OnListViewItemSelected;
            //this.MasterBehavior = MasterBehavior.SplitOnLandscape;
            this.Title = "DXGrid Demo";
        }

        void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e) {
            DemoContentPage content = Detail as DemoContentPage;
            if (content == null)
                return;

            ListView listView = sender as ListView;
            if (listView == null)
                return;

            content.BindingContext = listView.SelectedItem;
            DemoInfo selected = listView.SelectedItem as DemoInfo;
            if (selected != null) 
                this.Detail.Title = selected.Title;
            //this.IsPresented = true;
            if (this.MasterBehavior != MasterBehavior.SplitOnLandscape)
                this.IsPresented = false; // Show the detail page.
        }
    }
}