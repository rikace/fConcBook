using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Functional;
using System.Threading.Tasks;
using static System.Console;

namespace ReactiveAgent.Agents
{
    public static class WordCountAgentsExample
    {
        static IAgent<string> printer = Agent.Start((string msg) =>
            WriteLine($"{msg} on thread {Thread.CurrentThread.ManagedThreadId}"));

        //Listing 12.7  Producer/consumer using TPL Dataflow
        static IAgent<string> reader =
            Agent.Start(async (string filePath) =>
            {
                await printer.Send("reader received message");          // #A
                var lines = await File.ReadAllLinesAsync(filePath);     // #B
                lines.ForEach(async line => await parser.Send(line));   // #C
            });

        static char[] punctuation = Enumerable.Range(0, 256).Select(c => (char)c).Where(c => Char.IsWhiteSpace(c) || Char.IsPunctuation(c)).ToArray();

        static IAgent<string> parser =
            Agent.Start(async (string line) =>
            {
                await printer.Send("parser received message");          // #A
                line.Split(punctuation).ForEach(async word =>
                        await counter.Send(word.ToUpper()));     // #D
            });

        static IReplyAgent<string, (string, int)> counter =
            Agent.Start(ImmutableDictionary<string, int>.Empty,
                (ImmutableDictionary<string, int> state, string word) =>
                {
                    printer.Post("counter received message");      // #A

                    int count;
                    if (state.TryGetValue(word, out count))
                        return state.Add(word, count++);      // #E
                    else return state.Add(word, 1);
                }, (state, word) =>
                {
                    if (state.TryGetValue(word.ToUpper(), out int value))
                        return (state, (word, value));
                    else return (state, (word, 0));
                });

        public static async Task Run()
        {
            foreach (var filePath in Directory.EnumerateFiles(@".\Data", "*.txt"))
            {
                if (System.IO.File.Exists(filePath))
                    reader.Post(filePath);
            }

            Console.ReadLine();

            var wordCount_This = await counter.Ask("this");     // #F
            var wordCount_Wind = await counter.Ask("wind");     // #F
        }
    }
}
