using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace StockAnalyzer.CS
{

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var chart = new Chart { Dock = DockStyle.Fill };
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            var form = new Form { Visible = true, Width = 700, Height = 500 };
            form.Controls.Add(chart);

            var ctx = SynchronizationContext.Current;

            StockAnalyzer stockAnalyzer = new StockAnalyzer();

            Task.Factory.StartNew(async () => await stockAnalyzer.ProcessStockHistoryAsComplete(chart, ctx));
            Application.Run(form);
            Console.ReadLine();
        }
    }
}



