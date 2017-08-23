using StockTicker.Core;

namespace StockTicker.Client.Model
{
    public class StockModelObject : ModelObject
    {
        public StockModelObject(Models.Stock stock)
        {
            Update(stock);
        }

        public void Update(Models.Stock stock)
        {
            Symbol = stock.Symbol;
            Price = stock.Price;
            Change = stock.Change;
            LastChange = stock.LastChange;
            PercentChange = stock.PercentChange;
            DayHigh = stock.DayHigh;
            DayLow = stock.DayLow;
            DayOpen = stock.DayOpen;
            Index = stock.Index;
        }

        private string symbol;
        public string Symbol
        {
            get => symbol; set
            {
                if (symbol == value)
                    return;
                symbol = value;
                OnPropertyChanged();
            }
        }

        private double price;
        public double Price
        {
            get => price; set
            {
                if (price == value)
                    return;
                price = value;
                OnPropertyChanged();
            }
        }

        private double change;
        public double Change
        {
            get => change; set
            {
                if (change == value)
                    return;
                change = value;
                OnPropertyChanged();
            }
        }

        private double lastChange;
        public double LastChange
        {
            get => lastChange; set
            {
                if (lastChange == value)
                    return;
                lastChange = value;
                OnPropertyChanged();
            }
        }

        private double percentChange;
        public double PercentChange
        {
            get => percentChange; set
            {
                if (percentChange == value)
                    return;
                percentChange = value;
                OnPropertyChanged();
            }
        }

        private double dayHigh;
        public double DayHigh
        {
            get => dayHigh; set
            {
                if (dayHigh == value)
                    return;
                dayHigh = value;
                OnPropertyChanged();
            }
        }

        private double dayLow;
        public double DayLow
        {
            get => dayLow; set
            {
                if (dayLow == value)
                    return;
                dayLow = value;
                OnPropertyChanged();
            }
        }

        private double dayOpen;
        public double DayOpen
        {
            get => dayOpen; set
            {
                if (dayOpen == value)
                    return;
                dayOpen = value;
                OnPropertyChanged();
            }
        }

        private double index;
        public double Index
        {
            get => index; set
            {
                if (index == value)
                    return;
                index = value;
                OnPropertyChanged();
            }
        }
    }
}
