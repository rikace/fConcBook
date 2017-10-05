using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ColorPrint : IDisposable
    {
        private ConsoleColor old;

        public ColorPrint(ConsoleColor color)
        {
            old = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = old;
        }

        public static ConsoleColor GetColorForSymbol(string symbol)
        {
            switch (symbol)
            {
                case "msft":
                    return ConsoleColor.Cyan;
                case "aapl":
                    return ConsoleColor.Red;
                case "fb":
                    return ConsoleColor.Magenta;
                case "goog":
                    return ConsoleColor.Yellow;
                case "amzn":
                    return ConsoleColor.Green;
                default:
                    return Console.ForegroundColor;
            }
        }

        public static void PrintStockInfo(StockData stock)
        {
            ConsoleColor symbolColor = GetColorForSymbol(stock.Symbol.Substring(0, stock.Symbol.IndexOf('.')));
            using (new ColorPrint(symbolColor))
                Console.WriteLine(
                    $"{stock.Symbol} - Date {stock.Date.ToShortDateString()} - High Price {stock.High}");
        }
    }
}
