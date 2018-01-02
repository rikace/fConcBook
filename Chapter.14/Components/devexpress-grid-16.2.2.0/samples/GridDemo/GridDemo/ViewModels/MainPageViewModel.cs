using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mobile.DataGrid;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class MainPageViewModel : BindableObject {

        readonly Command refreshCommand;
        readonly OrdersRepository repository;
        readonly MarketSimulator market;

        ObservableCollection<Order> orders;

        public ObservableCollection<Order> Orders { 
            get { return orders; }
            set {
                if (orders != value) {
                    orders = value;
                    OnPropertyChanged("Orders");
                }
            }
        }
		public ObservableCollection<Customer> Customers { get { return repository.Customers; } }
        public ObservableCollection<Quote> Quotes { get { return market.Quotes; } }
		public Command SwipeButtonCommand { get; set; }
		public Command RefreshCommand { get { return refreshCommand; } }

        public MainPageViewModel(OrdersRepository repository) {
            this.repository = repository;
            Orders = repository.Orders;
			this.SwipeButtonCommand = new Command((o) => SwipeButtonClickExecute(o));
			this.refreshCommand = new Command(ExecuteRefreshCommand);
            this.market = new MarketSimulator();
        }

        void ExecuteRefreshCommand() {
            repository.RefreshOrders();
            Orders = repository.Orders;
        }
		void SwipeButtonClickExecute(object parameter) {
			SwipeButtonEventArgs arg = parameter as SwipeButtonEventArgs;
			if (arg != null) {
				if(arg.ButtonInfo.ButtonName == "RemoveButton") {
					this.Orders.RemoveAt(arg.SourceRowIndex);
				}
			}
		}

        bool marketSimulationOn;
        public void StartMarketSimulation() {
            if (marketSimulationOn)
                return;

            marketSimulationOn = true;
            Device.StartTimer(TimeSpan.FromSeconds(0.25), SimulateMarketWorker);
        }
        public void StopMarketSimulation() {
            this.marketSimulationOn = false;
        }

        bool SimulateMarketWorker() {
            if (!marketSimulationOn)
                return false;

            market.SimulateNextStep();
            return true;
        }
    }
}
