using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevExpress.GridDemo {
    public class Quote : ModelObject {
        string name = String.Empty;
        double currentValue;
        double previousValue;

        public string Name {
            get { return name; }
            set {
                if (Name == value)
                    return;

                this.name = value;
                RaisePropertyChanged("Name");
            }
        }
        public double CurrentValue {
            get { return currentValue; }
            set {
                if (CurrentValue == value)
                    return;

                this.currentValue = value;
                RaisePropertyChanged("CurrentValue");
            }
        }
        public double PreviousValue {
            get { return previousValue; }
            set {
                if (PreviousValue == value)
                    return;

                this.previousValue = value;
                RaisePropertyChanged("PreviousValue");
            }
        }

        public Quote Clone() {
            Quote result = new Quote();
            result.Name = this.Name;
            result.CurrentValue = this.CurrentValue;
            result.PreviousValue = this.PreviousValue;
            return result;
        }
    }

    public class MarketSimulator {
        readonly ObservableCollection<Quote> quotes;
        readonly DateTime now;
        readonly Random random;

        public MarketSimulator() {
            this.now = DateTime.Now;
            this.random = new Random((int)now.Ticks);
            this.quotes = new ObservableCollection<Quote>();
            PopulateQuotes();
        }

        public ObservableCollection<Quote> Quotes { get { return quotes; } }

        void PopulateQuotes() {
            quotes.Add(new Quote() { Name = "MSFT", CurrentValue = 42.01, PreviousValue = 42.01 });
            quotes.Add(new Quote() { Name = "GOOG", CurrentValue = 510.66, PreviousValue = 510.66 });
            quotes.Add(new Quote() { Name = "AAPL", CurrentValue = 118.9, PreviousValue = 118.9 });
            quotes.Add(new Quote() { Name = "IBM", CurrentValue = 155.48, PreviousValue = 155.48 });
            quotes.Add(new Quote() { Name = "HPQ", CurrentValue = 37.74, PreviousValue = 37.74 });
            quotes.Add(new Quote() { Name = "T", CurrentValue = 32.96, PreviousValue = 32.96 });
            quotes.Add(new Quote() { Name = "VZ", CurrentValue = 46.11, PreviousValue = 46.11 });
            quotes.Add(new Quote() { Name = "YHOO", CurrentValue = 43.73, PreviousValue = 43.73 });
        }

        public void SimulateNextStep() {
            int index = random.Next(0, quotes.Count);
            UpdateQuoteNotify(index);
        }

        void UpdateQuoteNotify(int index) {
            Quote quote = quotes[index].Clone();
            UpdateQuote(quote);
            quotes[index] = quote;
        }
        void UpdateQuote(Quote quote) {
            double value = quote.CurrentValue;

            quote.PreviousValue = value;

            int percentChange = random.Next(0, 201) - 100;
            double newValue = value + value * (5 * percentChange / 10000.0); // single change should be change less than 5%
            if (newValue < 0)
                newValue = value - value * (5 * percentChange / 10000.0); // single change should be change less than 5%

            quote.CurrentValue = newValue;
        }
    }
}
