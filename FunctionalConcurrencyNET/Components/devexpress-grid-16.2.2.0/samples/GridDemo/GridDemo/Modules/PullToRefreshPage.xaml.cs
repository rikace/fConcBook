using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DevExpress.Mobile.DataGrid;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class PullToRefreshPage {
		public static readonly BindableProperty OrdersProperty = BindableProperty.Create("Orders", typeof(ObservableCollection<Order>), typeof(PullToRefreshPage), null);
		public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create("PullToRefreshCommand", typeof(ICommand), typeof(PullToRefreshPage), null);
        
        OrdersRepository repository;

        public Command PullToRefreshCommand {
            get { return (Command)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }

        public ObservableCollection<Order> Orders {
            get { return (ObservableCollection<Order>)GetValue(OrdersProperty); }
            set { SetValue(OrdersProperty, value); }
        }

        public PullToRefreshPage() {
            InitializeComponent();
            BindData();
        }

        void BindData() {
            BindingContext = this;
            this.repository = new DemoOrdersRepository();
            this.Orders = repository.Orders;
            PullToRefreshCommand = new Command(ExecutePullToRefreshCommand);
        }

        void ExecutePullToRefreshCommand() {
            repository.RefreshOrders();
            Orders = repository.Orders;
        }
    }
}
