using ParallelCompressionCS;
using ReactiveAgent.Agents;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ReactiveAgent.Agents.Dataflow;
using System.Collections.Generic;
using FSharp.Charting;
using Microsoft.FSharp.Core;
using System.Windows.Forms;
using ReactiveAgent.CS;

namespace ParallelCompressionDataFlowAgent
{
    public class Program
    {
        static void RunPerfComparison() =>
            RunPerfComparison(new[] { 1, 3, 9 }, new[] { 1, 2, 4 });

        public static void Main(string[] args)
        {
            //Play().Wait();

            RunPerfComparison();
        }

        static async Task Play()
        {
            // 12.1
            await (new DataflowBufferBlock()).Run();
            // 12.2, 12.3
            (new DataflowTransformActionBlocks()).Run();
            // 12.4
            await (new MultipleProducersExample()).Run();
            // 12.5, 12.6
            (new StatefulDataflowAgentSample()).Run();
        }

        static void CreateTextFileWithSize(int size_Mb, string destination)
        {
            string base_filePath = Path.Combine(workDirectory, source_base_file);
            var bytes = System.IO.File.ReadAllBytes(base_filePath);
            int targetSize = size_Mb * 1024 * 1024;
            using (FileStream fs = new FileStream(destination, FileMode.Append, FileAccess.Write))
            {
                var iterations = (targetSize - fs.Length + bytes.Length) / bytes.Length;

                for (var i = 0; i < iterations; i++)
                    fs.Write(bytes, 0, bytes.Length);
            }
        }

        private static string workDirectory = @".\Data";
        private static string source_base_file = "base_text.txt";

        public static void CompressAndEncrypt(string srcFile, string dstFile, string rstFile)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                Console.WriteLine("CompressAndEncrypt ...");
                using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
                using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
                {
                    CompressionAndEncryptDataFlow.CompressAndEncrypt(streamSource, streamDestination).Wait();
                    streamDestination.Close();
                }
                Console.WriteLine($"Done in {sw.ElapsedMilliseconds}");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
                sw.Restart();

                Console.WriteLine("DecryptAndDecompress ...");
                using (var streamSource = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
                using (var streamDestination = new FileStream(rstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
                    CompressionAndEncryptDataFlow.DecryptAndDecompress(streamSource, streamDestination).Wait();
                Console.WriteLine($"Done in {sw.ElapsedMilliseconds}");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();

                Console.WriteLine("Verification ...");
                using (var f1 = System.IO.File.OpenRead(srcFile))
                using (var f2 = System.IO.File.OpenRead(rstFile))
                {
                    bool ok = false;
                    if (f1.Length == f2.Length)
                    {
                        ok = true;
                        int count;
                        const int size = 0x1000000;

                        var buffer = new byte[size];
                        var buffer2 = new byte[size];

                        while ((count = f1.Read(buffer, 0, buffer.Length)) > 0 && ok == true)
                        {
                            f2.Read(buffer2, 0, count);
                            ok = buffer2.SequenceEqual(buffer);
                            if (!ok) break;
                        }
                    }
                    Console.WriteLine($"Restored file isCorrect = {ok}");
                }

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
            }
            catch (AggregateException ex)
            {
                var q = new Queue<Exception>(new[] { ex });
                while (q.Count > 0)
                {
                    var e = q.Dequeue();
                    Console.WriteLine($"\t{e.Message}");
                    if (e is AggregateException)
                    {
                        foreach (var x in (e as AggregateException).InnerExceptions)
                            q.Enqueue(x);
                    }
                    else
                    {
                        if (e.InnerException != null)
                            q.Enqueue(e.InnerException);
                    }
                }
            }
        }

        public static void RunPerfComparison(int[] fileSizesInGb, int[] degreesOfParallelism)
        {
            const int Mb_to_Gb = 1024;

            string inFile = Path.Combine(workDirectory, "inFile.txt");
            string outFile = Path.Combine(workDirectory, "outFile.txt");

            var results = new List<List<TimeSpan>>();

            foreach (var size in fileSizesInGb)
            {
                Console.WriteLine($"Creating input file {size}GB ...");
                if (System.IO.File.Exists(inFile))
                    System.IO.File.Delete(inFile);

                CreateTextFileWithSize(Mb_to_Gb * size, inFile);

                for (var i = 0; i < degreesOfParallelism.Length; i++)
                {
                    var dop = degreesOfParallelism[i];
                    if (System.IO.File.Exists(outFile))
                        System.IO.File.Delete(outFile);

                    Console.WriteLine($"Running compression with degreeOfParallelism={dop} ...");
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    RunCompression(inFile, outFile, dop);
                    sw.Stop();
                    Console.WriteLine($"\t Elapsed = {sw.Elapsed}");

                    if (i == results.Count)
                        results.Add(new List<TimeSpan>());
                    results[i].Add(sw.Elapsed);
                }
            }

            var keys = fileSizesInGb.Select(s => $"{s}Gb").ToList();
            var charts = new List<ChartTypes.GenericChart>();
            for (var i = 0; i < results.Count; i++)
            {
                var line = results[i].Select(t => t.TotalMilliseconds).ToList();
                var lineChart = PerfVis.CreateLineChart(line, keys, $"dop={degreesOfParallelism[i]}");
                charts.Add(lineChart);
            }
            Application.Run(PerfVis.CombineToForm(charts, "Dataflow compression with different degree of parallelism(dop)"));
        }

        public static void RunCompression(string srcFile, string dstFile, int degreeOfParallelism)
        {
            using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
            using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
            {
                CompressionAndEncryptDataFlow.CompressAndEncrypt(streamSource, streamDestination, degreeOfParallelism).Wait();
                streamDestination.Close();
            }
        }

        // 300 MB
        static void CreateTextFileWithSize_300Mb() =>
                CreateTextFileWithSize(300, Path.Combine(workDirectory, "txt_3Gb.text"));

        // 3 GB
        static void CreateTextFileWithSize_3Gb() =>
                CreateTextFileWithSize(3 * 1024, Path.Combine(workDirectory, "txt_3Gb.text"));

        // 6 GB
        static void CreateTextFileWithSize_6GB() =>
                CreateTextFileWithSize(6 * 1024, Path.Combine(workDirectory, "txt_6Gb.text"));

        // 12 Gb
        static void CreateTextFileWithSize_12GB() =>
            CreateTextFileWithSize(12 * 1024, Path.Combine(workDirectory, "txt_12Gb.text"));

    }
}