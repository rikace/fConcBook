using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public partial class LoadMorePage {
		public static readonly BindableProperty OrdersProperty = BindableProperty.Create("Orders", typeof(ObservableCollection<Order>), typeof(LoadMorePage), null);
		public static readonly BindableProperty LoadMoreCommandProperty = BindableProperty.Create("LoadMoreCommand", typeof(LoadMoreDataCommand), typeof(LoadMorePage), null);

        OrdersRepository repository;

        public LoadMoreDataCommand LoadMoreCommand {
            get { return (LoadMoreDataCommand)GetValue(LoadMoreCommandProperty); }
            set { SetValue(LoadMoreCommandProperty, value); }
        }

        public ObservableCollection<Order> Orders {
            get { return (ObservableCollection<Order>)GetValue(OrdersProperty); }
            set { SetValue(OrdersProperty, value); }
        }

        public LoadMorePage() {
            InitializeComponent();
            BindData();
        }

        void BindData() {
            BindingContext = this;
            this.repository = new DemoOrdersRepository();
            this.Orders = repository.Orders;
            LoadMoreCommand = new LoadMoreDataCommand(ExecuteLoadMoreCommand);
        }

        void ExecuteLoadMoreCommand() {
            repository.LoadMoreOrders();
            Orders = repository.Orders;
        }
    }

    public class LoadMoreDataCommand : ICommand {
        readonly Action execute;

        int numOfLoadMore;
        public event EventHandler CanExecuteChanged;
        bool canExecute = true;

        public LoadMoreDataCommand(Action execute) {
            this.execute = execute;
        }

        public bool CanExecute(object parameter) {
            return canExecute;
        }

        public void Execute(object parameter) {
            numOfLoadMore++;
            if (numOfLoadMore < 3) {
                ChangeCanExecute(true);
                this.execute();
            } else {
                ChangeCanExecute(false);
                TryDownloadAgain();
            }
        }

        async void TryDownloadAgain() {
            await Task.Delay(5000);
            numOfLoadMore = 0;
            ChangeCanExecute(true);
        }
        void ChangeCanExecute(bool canExecute) {
            this.canExecute = canExecute;
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, new EventArgs());
        }
    }
}
