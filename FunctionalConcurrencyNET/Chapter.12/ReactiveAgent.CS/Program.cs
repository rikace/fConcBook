using ParallelCompressionCS;
using ReactiveAgent.Agents;
using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveAgent.Agents.Dataflow;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using FSharp.Charting;
using Microsoft.FSharp.Core;
using System.Windows.Forms;
using File = ReactiveAgent.Agents.File;

namespace ReactiveAgent.CS
{
    public class StatefulDataflowAgentSample
    {
        public void Run()
        {
            // Listing 12.6  Producer/consumer using TPL Dataflow
            List<string> urls = new List<string> {
                @"http://www.google.com",
                @"http://www.microsoft.com",
                @"http://www.bing.com",
                @"http://www.google.com"
            };

            var agentStateful = Agent.Start(ImmutableDictionary<string, string>.Empty,
                async (ImmutableDictionary<string, string> state, string url) =>
                {    // #A
                    if (!state.TryGetValue(url, out string content))
                        using (var webClient = new WebClient())
                        {
                            content = await webClient.DownloadStringTaskAsync(url);
                            await File.WriteAllTextAsync(createFileNameFromUrl(url), content);
                            return state.Add(url, content);   // #B
                        }
                    return state;        // #B
                });

            urls.ForEach(url => agentStateful.Post(url));


            // Agent fold over state and messages - Aggregate
            urls.Aggregate(ImmutableDictionary<string, string>.Empty,
                (state, url) => {
                    if (!state.TryGetValue(url, out string content))
                        using (var webClient = new WebClient())
                        {
                            content = webClient.DownloadString(url);
                            System.IO.File.WriteAllText(createFileNameFromUrl(url), content);
                            return state.Add(url, content);
                        }
                    return state;
                });
        }

        public string createFileNameFromUrl(string url)
        {
            return Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Play().Wait();
        }

        static async Task Play()
        {
            // TODO
            // add local text files
            await (new WordCountAgentsExample()).Run();
        }
    }
}