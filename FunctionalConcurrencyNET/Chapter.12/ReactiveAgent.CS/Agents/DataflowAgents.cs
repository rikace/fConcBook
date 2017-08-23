using Functional;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.Agents.Dataflow
{
    //Listing 12.5 An Agent in C# using TPL Dataflow
    public class StatefulDataflowAgent<TState, TMessage> : IAgent<TMessage>
    {
        private TState state;
        private readonly ActionBlock<TMessage> actionBlock;

        public StatefulDataflowAgent(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action, // #A
            CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ?
                    cts.Token : CancellationToken.None  // #B
            };
            actionBlock = new ActionBlock<TMessage>(    // #C
                async msg => state = await action(state, msg), options);
        }

        public Task Send(TMessage message) => actionBlock.SendAsync(message);
        public void Post(TMessage message) => actionBlock.Post(message);


        public StatefulDataflowAgent(TState initialState, Func<TState, TMessage, TState> action, CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts == null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(
                msg => state = action(state, msg), options);
        }

        public TState State => state;
    }


    public class StatelessDataflowAgent<TMessage> : IAgent<TMessage>
    {
        private readonly ActionBlock<TMessage> actionBlock;

        public StatelessDataflowAgent(Action<TMessage> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts == null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public StatelessDataflowAgent(Func<TMessage, Task> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts == null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public void Post(TMessage message) => actionBlock.Post(message);
        public Task Send(TMessage message) => actionBlock.SendAsync(message);

    }



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
               async (ImmutableDictionary<string, string>  state, string url) =>
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

}

