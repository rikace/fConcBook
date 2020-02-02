using System;
using System.Threading.Tasks;

namespace StockAnalyzer.CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // this is not needed since we are using browser based charting
            // var ctx = SynchronizationContext.Current;

            var stockAnalyzer = new StockAnalyzer();

            Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryAsComplete());

            Console.ReadLine();
        }
    }
}