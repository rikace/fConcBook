using DevExpress.Mobile.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public abstract class OrdersRepository {
        readonly ObservableCollection<Customer> customers;

        public ObservableCollection<Order> Orders { get; private set; }
        public ObservableCollection<Customer> Customers { get { return customers; } }

        public OrdersRepository() {
            this.Orders = new ObservableCollection<Order>();
            this.customers = new ObservableCollection<Customer>();
        }

        protected abstract Order GenerateOrder(int number);
        protected abstract int GetOrderCount();

        internal void LoadMoreOrders() {
            ObservableCollection<Order> newOrders = new ObservableCollection<Order>();
            foreach (var order in Orders)
                newOrders.Add(order);

            for (int i = 0; i < GetOrderCount(); i++)
                newOrders.Add(GenerateOrder(i));

            Orders = newOrders;
        }
        internal void RefreshOrders() {
            ObservableCollection<Order> newOrders = new ObservableCollection<Order>();
            for (int i = 0; i < GetOrderCount(); i++)
                newOrders.Add(GenerateOrder(i));

            Orders = newOrders;
        }
    }
}

