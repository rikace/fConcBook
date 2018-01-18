using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataParallelism.cs
{
    class Program
    {
        static void Mandelbrot_Performance_Comparison()
        {
            Demo.PrintSeparator();
            Console.WriteLine("Mandelbrot Performance Comparison");
            Func<Func<Bitmap>, Action[]> run = (func) =>
                    new Action[] { () => { func(); } };

            var implementations =
                new[]
                {
                   new Tuple<String, Action[]>(
                       "C# Sequential", run(Mandelbrot.SequentialMandelbrot)),
                   new Tuple<String, Action[]>(
                       "C# Parallel.For", run(Mandelbrot.ParallelMandelbrot)),
                   new Tuple<String, Action[]>(
                       "C# Parallel.For Saturated", run(Mandelbrot.ParallelMandelbrotOversaturation)),
                   new Tuple<String, Action[]>(
                       "C# Parallel.For Struct", run(Mandelbrot.ParallelStructMandelbrot))
                };

            Application.Run(
                PerfVis.toChart("C# Mandelbrot")
                    .Invoke(PerfVis.fromTuples(implementations)));
        }

        static void Prime_Sum()
        {
            Demo.PrintSeparator();
            Console.WriteLine("Prime Sum [0..10^7]");
            Func<Func<long>, Action[]> runSum = (func) =>
                new Action[]
                {
                    () =>
                    {
                        var result = func();
                        Console.WriteLine($"Sum = {result}");
                    }
                };

            var sumImplementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "C# Sequential", runSum(PrimeNumbers.PrimeSumSequential)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For", runSum(PrimeNumbers.PrimeSumParallel)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For ThreadLocal", runSum(PrimeNumbers.PrimeSumParallelThreadLocal)),
                    new Tuple<String, Action[]>(
                        "C# Parallel LINQ", runSum(PrimeNumbers.PrimeSumParallelLINQ))
                };
            Application.Run(
                PerfVis.toChart("C# Prime Sum")
                    .Invoke(PerfVis.fromTuples(sumImplementations)));
        }

        [STAThread]
        static void Main(string[] args)
        {
            Mandelbrot_Performance_Comparison();
            // Prime_Sum();

            Console.ReadLine();
        }
    }
}
