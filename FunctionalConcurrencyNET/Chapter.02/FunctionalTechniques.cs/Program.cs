using FunctionalTechniques.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionalTechniques
{
    class Program
    {

        static void Main(string[] args)
        {
            Closure closure = new Closure();
            closure.Closure_Strange_Behavior();
            Demo.PrintSeparator();
            closure.Closure_Correct_Behavior();
            Demo.PrintSeparator();

            Memoization.RunDemo();
            Demo.PrintSeparator();

            WebCrawlerExample.RunDemo();
            Demo.PrintSeparator();

            ConcurrentSpeculation.FuzzyMatchDemo();
            Demo.PrintSeparator();

            Person.RunDemo();
        }
    }
}
