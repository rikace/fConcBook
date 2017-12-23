using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.CS
{
    class DataflowBufferBlock
    {
        //Listing 12.1 Simple Producer Consumer using TPL Dataflow BufferBlock
        BufferBlock<int> buffer = new BufferBlock<int>(); // #A

        async Task Producer(IEnumerable<int> values)
        {
            foreach (var value in values)
                await buffer.SendAsync(value);    // #B
            buffer.Complete();         // #C
        }

        async Task Consumer(Action<int> process)
        {
            while (await buffer.OutputAvailableAsync()) // #D
                process(await buffer.ReceiveAsync());   // #E
        }

        public async Task Run()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);
            await Task.WhenAll(Producer(range), Consumer(n =>
                Console.WriteLine($"value {n}")));
        }
    }

    class DataflowTransformActionBlocks
    {
        public void Run()
        {
            //Listing 12.2 Download image using TPL Dataflow TransformBlock
            var fetchImageFlag = new TransformBlock<string, (string, byte[])>(
                async urlImage =>
                { // #A
                    using (var webClient = new WebClient())
                    {
                        byte[] data = await webClient.DownloadDataTaskAsync(urlImage); // #B
                        return (urlImage, data);
                    }  // #C
                });

            List<string> urlFlags = new List<string>{
                "Italy#/media/File:Flag_of_Italy.svg",
                "Spain#/media/File:Flag_of_Spain.svg",
                "United_States#/media/File:Flag_of_the_United_States.svg"
                };

            foreach (var urlFlag in urlFlags)
                fetchImageFlag.Post($"https://en.wikipedia.org/wiki/{urlFlag}");
            
            //Listing 12.3 Persist data using TPL Dataflow ActionBlock
            var saveData = new ActionBlock<(string, byte[])>(async data =>
            { // #A
                (string urlImage, byte[] image) = data; // #B
                string filePath = urlImage.Substring(urlImage.IndexOf("File:") + 5);
                await Agents.File.WriteAllBytesAsync(filePath, image); // #C
            });

            fetchImageFlag.LinkTo(saveData);    // #D
        }
    }

    class MultipleProducersExample
    {
        //Listing 12.4 Asynchronous producer/consumer using TPL Dataflow
        BufferBlock<int> buffer = new BufferBlock<int>(
            new DataflowBlockOptions { BoundedCapacity = 10 }); // #A

        async Task Produce(IEnumerable<int> values)
        {
            foreach (var value in values)
                await buffer.SendAsync(value); ;    // #B
        }

        async Task MultipleProducers(params IEnumerable<int>[] producers)
        {
            await Task.WhenAll(
                    (from values in producers select Produce(values)).ToArray())  // #C
                .ContinueWith(_ => buffer.Complete());  // #D
        }

        async Task Consumer(Action<int> process)
        {
            while (await buffer.OutputAvailableAsync()) // #E
                process(await buffer.ReceiveAsync());
        }

        public async Task Run()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);

            await Task.WhenAll(MultipleProducers(range, range, range),
                Consumer(n => Console.WriteLine($"value {n} - ThreadId{Thread.CurrentThread.ManagedThreadId}")));
        }
    }
}