using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class GroupDatesPage {
        ColumnGroupInterval[] intervals;

		public static readonly BindableProperty SelectedIntervalProperty = BindableProperty.Create("SelectedInterval", typeof(ColumnGroupInterval), typeof(GroupDatesPage), ColumnGroupInterval.Default, BindingMode.OneWay, null, (d, o, n) => { ((GroupDatesPage)d).OnColumnGroupIntervalChanged(); });
		public static readonly BindableProperty OrdersProperty = BindableProperty.Create("Orders", typeof(ObservableCollection<Order>), typeof(GroupDatesPage), null);

        public GroupDatesPage() {
            intervals = new ColumnGroupInterval[] {
                ColumnGroupInterval.DateRange,
                ColumnGroupInterval.Date,
                ColumnGroupInterval.DateMonth,
                ColumnGroupInterval.DateQuarter,
                ColumnGroupInterval.DateYear
            };

            InitializeComponent();
            SelectedInterval = intervals[0];
            OnColumnGroupIntervalChanged();
            BindData();
        }

        public ColumnGroupInterval[] Intervals { get { return intervals; } }
        public ColumnGroupInterval SelectedInterval {
            get { return (ColumnGroupInterval)GetValue(SelectedIntervalProperty); }
            set { SetValue(SelectedIntervalProperty, value); }
        }
        public ObservableCollection<Order> Orders {
            get { return (ObservableCollection<Order>)GetValue(OrdersProperty); }
            set { SetValue(OrdersProperty, value); }
        }

        void OnColumnGroupIntervalChanged() {
            colDate.SortOrder = SelectedInterval == ColumnGroupInterval.DateRange ? ColumnSortOrder.Descending : ColumnSortOrder.Ascending;
            colDate.GroupInterval = SelectedInterval;
        }

        async void BindData() {
            this.BindingContext = this;

            MainPageViewModel model =  await LoadData();
            this.Orders = model.Orders;
        }
        Task<MainPageViewModel> LoadData() {
            return Task<MainPageViewModel>.Run(() => new MainPageViewModel(new DemoOrdersRepository()));
        }
    }
}
