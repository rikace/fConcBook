using System;

namespace Common
{
    [Serializable]
    public class StockData
    {
        public StockData(string symbol, DateTime date, double open, double high, double low, double close)
        {
            Symbol = symbol;
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public string Symbol { get; }
        public DateTime Date { get; set; }
        public double Open { get; }
        public double High { get; }
        public double Low { get; }
        public double Close { get; }

        public static StockData Parse(string symbol, string row)
        {
            if (string.IsNullOrWhiteSpace(row))
                return null;

            var cells = row.Split(',');
            if (!DateTime.TryParse(cells[0], out var date))
                return null;

            var open = ParseDouble(cells[1]);
            var high = ParseDouble(cells[2]);
            var low = ParseDouble(cells[3]);
            var close = ParseDouble(cells[4]);
            return new StockData(symbol, date, open, high, low, close);
        }

        private static double ParseDouble(string s)
        {
            return double.TryParse(s, out var x) ? x : -1;
        }
    }
}