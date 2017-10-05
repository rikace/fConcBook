using FuzzyMatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionalTechniques.cs
{
    struct Temperature
    {
        Temperature(float temperature)
        {
            Temp = temperature;
        }

        public float Temp { get; }

    }
    class ConcurrentSpeculation
    {
        // Listing 2.22 A fuzzy match
        public static string FuzzyMatch(List<string> words, string word)
        {
            var wordSet = new HashSet<string>(words);   //#A

            string bestMatch =
                (from w in wordSet.AsParallel()         //#B
                 select JaroWinklerModule.Match(w, word))
                    .OrderByDescending(w => w.Distance)
                    .Select(w => w.Word)
                    .FirstOrDefault();
            return bestMatch;                           //#C
        }

        // Listing 2.23 Fast Fuzzy Match using precomputation
        public static Func<string, string> PartialFuzzyMatch(List<string> words) //#A
        {
            var wordSet = new HashSet<string>(words);   //#B

            return word =>
                (from w in wordSet.AsParallel()
                 select JaroWinklerModule.Match(w, word))
                    .OrderByDescending(w => w.Distance)
                    .Select(w => w.Word)
                    .FirstOrDefault();                  //#C
        }

        public static void FuzzyMatchDemo()
        {
            List<string> words =
                System.IO.File
                    .ReadAllLines("google-10000-english/google-10000-english.txt")
                    .Where(x=>
                        !(x.Equals("magic", StringComparison.InvariantCultureIgnoreCase)) &&
                        !(x.Equals("light", StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();

            // Listing 2.22 A fuzzy match
            Demo.Benchmark("Listing 2.22 A fuzzy match", () =>
            {
                string fuzzyMatch = ConcurrentSpeculation.FuzzyMatch(words, "magic"); //#D

                Console.WriteLine($"FuzzyMatch for 'magic' = {fuzzyMatch}");
            });

            Demo.Benchmark("Listing 2.23 Fast Fuzzy Match using precomputation", () =>
            {
                Func<string, string> fastFuzzyMatch = PartialFuzzyMatch(words); //#D

                string magicFuzzyMatch = fastFuzzyMatch("magic");
                string lightFuzzyMatch = fastFuzzyMatch("light");   //#E

                Console.WriteLine($"FastFuzzyMatch for 'magic' = {magicFuzzyMatch}");
                Console.WriteLine($"FastFuzzyMatch for 'light' = {lightFuzzyMatch}");
            });
        }


        // Listing 2.25 Fastest weather task
        public Temperature SpeculativeTempCityQuery(string city, params Uri[] weatherServices)
        {
            var cts = new CancellationTokenSource();    //#A
            var tasks =
            (from uri in weatherServices
             select Task.Factory.StartNew<Temperature>(() =>
                        queryService(uri, city), cts.Token)).ToArray(); //#B

            int taskIndex = Task.WaitAny(tasks);        //#C
            Temperature tempCity = tasks[taskIndex].Result;
            cts.Cancel();                               //#D
            return tempCity;
        }

        private Temperature queryService(Uri uri, string city)
        {
            throw new NotImplementedException();
        }
    }
}
