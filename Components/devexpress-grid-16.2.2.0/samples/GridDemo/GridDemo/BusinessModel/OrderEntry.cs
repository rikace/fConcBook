using System;

using Xamarin.Forms;

namespace DevExpress.GridDemo {
    public class OrderEntry : ModelObject {
        Commodity commodity;
        double amount;
        double price;
        double total;

        public Commodity Commodity {
            get { return commodity; }
            set { 
                commodity = value; 
                RaisePropertyChanged("Commodity");
            }
        }
        public double Amount {
            get { return amount; }
            set { 
                amount = value; 
                RaisePropertyChanged("Amount");
                UpdateTotal(raiseChanged: true);
            }
        }
        public double Price {
            get { return amount; }
            set { 
                amount = value; 
                RaisePropertyChanged("Price");
                UpdateTotal(raiseChanged: true);
            }
        }
        public double Total {
            get { return total; }
        }

        public OrderEntry(Commodity commodity, double amount, double price) {
            this.commodity = commodity;
            this.amount = amount;
            this.price = price;
            UpdateTotal(raiseChanged: false);
        }

        void UpdateTotal(bool raiseChanged) {
            total = price * amount;
            if (raiseChanged)
                RaisePropertyChanged("Total");
        }
    }
}

