using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataParallelism.Part2.CSharp
{
    public static class WordsCounterDemo
    {
        // Listing 5.1 Parallel Words Counter program with side effects
        public static Dictionary<string, int> WordsCounter(string source)
        {
            var wordsCount =
                    (from filePath in
                        Directory.GetFiles(source, "*.txt") //#A
                                 .AsParallel() //#B
                     from line in File.ReadLines(filePath)
                     from word in line.Split(' ')
                     select word.ToUpper()) //#C
                .GroupBy(w => w)
                .OrderByDescending(v => v.Count()).Take(10); //#D
            return wordsCount.ToDictionary(k => k.Key, v => v.Count());
        }

// Listing 5.3 Decouple and isolate side effects
public static Dictionary<string, int> PureWordsPartitioner(IEnumerable<IEnumerable<string>> content) =>
    (from lines in content.AsParallel() //#B
        from line in lines
        from word in line.Split(' ')
        select word.ToUpper())
        .GroupBy(w => w)        
            .OrderByDescending(v => v.Count()).Take(10)
            .ToDictionary(k => k.Key, v => v.Count());

public static Dictionary<string, int> WordsPartitioner(string source)
{
    var contentFiles =
        (from filePath in Directory.GetFiles(source, "*.txt")
            let lines = File.ReadLines(filePath)
            select lines);

    return PureWordsPartitioner(contentFiles);
}


        public static void Demo()
        {
            var dataPath = @"Shakespeare";

            Func<Func<string, Dictionary<string, int>>, Action[]> run = (func) =>
                    new Action[] { () => { func(dataPath); } };

            var implementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "WordCounter", run(WordsCounter)),
                    new Tuple<String, Action[]>(
                        "Pure WordCounter", run(WordsPartitioner))
                };

            Application.Run(
                PerfVis.toChart("WordCount")
                    .Invoke(PerfVis.fromTuples(implementations)));
        }
    }
}
