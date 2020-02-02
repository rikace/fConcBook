using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using static BenchmarkUtils.Benchmark;

namespace FunctionalTechniques.CSharp
{
    internal struct Temperature
    {
        private Temperature(float temperature)
        {
            Temp = temperature;
        }

        public float Temp { get; }
    }

    internal class ConcurrentSpeculation
    {
        // Listing 2.22 A fuzzy match
        public static string FuzzyMatch(List<string> words, string word)
        {
            var wordSet = new HashSet<string>(words); //#A

            var bestMatch =
                (from w in wordSet.AsParallel() //#B
                    select JaroWinklerModule.Match(w, word))
                .OrderByDescending(w => w.Distance)
                .Select(w => w.Word)
                .FirstOrDefault();
            return bestMatch; //#C
        }

        // Listing 2.23 Fast Fuzzy Match using pre-computation
        public static Func<string, string> PartialFuzzyMatch(List<string> words) //#A
        {
            var wordSet = new HashSet<string>(words); //#B

            return word =>
                (from w in wordSet.AsParallel()
                    select JaroWinklerModule.Match(w, word))
                .OrderByDescending(w => w.Distance)
                .Select(w => w.Word)
                .FirstOrDefault(); //#C
        }

        public static void FuzzyMatchDemo()
        {
            var words =
                File
                    .ReadAllLines("google-10000-english/google-10000-english.txt")
                    .Where(x =>
                        !x.Equals("magic", StringComparison.InvariantCultureIgnoreCase) &&
                        !x.Equals("light", StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            // Listing 2.22 A fuzzy match
            Bench.Time("Listing 2.22 A fuzzy match", () =>
            {
                var fuzzyMatch = FuzzyMatch(words, "magic"); //#D

                Console.WriteLine($"FuzzyMatch for 'magic' = {fuzzyMatch}");
            });

            Bench.Time("Listing 2.23 Fast Fuzzy Match using precomputation", () =>
            {
                var fastFuzzyMatch = PartialFuzzyMatch(words); //#D

                var magicFuzzyMatch = fastFuzzyMatch("magic");
                var lightFuzzyMatch = fastFuzzyMatch("light"); //#E

                Console.WriteLine($"FastFuzzyMatch for 'magic' = {magicFuzzyMatch}");
                Console.WriteLine($"FastFuzzyMatch for 'light' = {lightFuzzyMatch}");
            });
        }


        // Listing 2.25 Fastest weather task
        public Temperature SpeculativeTempCityQuery(string city, params Uri[] weatherServices)
        {
            var cts = new CancellationTokenSource(); //#A
            var tasks =
                (from uri in weatherServices
                    select Task.Factory.StartNew(() =>
                        queryService(uri, city), cts.Token)).ToArray(); //#B

            var taskIndex = Task.WaitAny(tasks); //#C
            var tempCity = tasks[taskIndex].Result;
            cts.Cancel(); //#D
            return tempCity;
        }

        private Temperature queryService(Uri uri, string city)
        {
            throw new NotImplementedException();
        }
    }
}