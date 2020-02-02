using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReactiveAgent.CSharp;
using ReactiveAgent.CSharp.ParallelCompression;
using Utilities;
using File = System.IO.File;

namespace ParallelCompressionDataFlowAgent
{
    public class Program
    {
        private static readonly string workDirectory = @".\Data";
        private static readonly string source_base_file = "base_text.txt";

        private static void RunPerfComparison()
        {
            RunPerfComparison(new[] {1, 3, 9}, new[] {1, 2, 4});
        }

        public static void Main(string[] args)
        {
            //Play().Wait();
            RunPerfComparison();
        }

        private static async Task Play()
        {
            // 12.1
            await new DataflowBufferBlock().Run();
            // 12.2, 12.3
            new DataflowTransformActionBlocks().Run();
            // 12.4
            await new MultipleProducersExample().Run();
            // 12.5, 12.6
            new StatefulDataflowAgentSample().Run();
        }

        private static void CreateTextFileWithSize(int size_Mb, string destination)
        {
            var base_filePath = Path.Combine(workDirectory, source_base_file);
            var bytes = File.ReadAllBytes(base_filePath);
            var targetSize = size_Mb * 1024 * 1024;
            using (var fs = new FileStream(destination, FileMode.Append, FileAccess.Write))
            {
                var iterations = (targetSize - fs.Length + bytes.Length) / bytes.Length;

                for (var i = 0; i < iterations; i++)
                    fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static void CompressAndEncrypt(string srcFile, string dstFile, string rstFile)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                Console.WriteLine("CompressAndEncrypt ...");
                using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read,
                    FileShare.None, 0x1000, true))
                using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write,
                    FileShare.None, 0x1000, true))
                {
                    CompressionAndEncryptDataFlow.CompressAndEncrypt(streamSource, streamDestination).Wait();
                    streamDestination.Close();
                }

                Console.WriteLine($"Done in {sw.ElapsedMilliseconds}");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
                sw.Restart();

                Console.WriteLine("DecryptAndDecompress ...");
                using (var streamSource = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Read,
                    FileShare.None, 0x1000, true))
                using (var streamDestination = new FileStream(rstFile, FileMode.OpenOrCreate, FileAccess.Write,
                    FileShare.None, 0x1000, true))
                {
                    CompressionAndEncryptDataFlow.DecryptAndDecompress(streamSource, streamDestination).Wait();
                }

                Console.WriteLine($"Done in {sw.ElapsedMilliseconds}");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();

                Console.WriteLine("Verification ...");
                using (var f1 = File.OpenRead(srcFile))
                using (var f2 = File.OpenRead(rstFile))
                {
                    var ok = false;
                    if (f1.Length == f2.Length)
                    {
                        ok = true;
                        int count;
                        const int size = 0x1000000;

                        var buffer = new byte[size];
                        var buffer2 = new byte[size];

                        while ((count = f1.Read(buffer, 0, buffer.Length)) > 0 && ok)
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
                var q = new Queue<Exception>(new[] {ex});
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

            var inFile = Path.Combine(workDirectory, "inFile.txt");
            var outFile = Path.Combine(workDirectory, "outFile.txt");

            var results = new List<List<TimeSpan>>();

            foreach (var size in fileSizesInGb)
            {
                Console.WriteLine($"Creating input file {size}GB ...");
                if (File.Exists(inFile))
                    File.Delete(inFile);

                CreateTextFileWithSize(Mb_to_Gb * size, inFile);

                for (var i = 0; i < degreesOfParallelism.Length; i++)
                {
                    var dop = degreesOfParallelism[i];
                    if (File.Exists(outFile))
                        File.Delete(outFile);

                    Console.WriteLine($"Running compression with degreeOfParallelism={dop} ...");
                    var sw = Stopwatch.StartNew();
                    RunCompression(inFile, outFile, dop);
                    sw.Stop();
                    Console.WriteLine($"\t Elapsed = {sw.Elapsed}");

                    if (i == results.Count)
                        results.Add(new List<TimeSpan>());
                    results[i].Add(sw.Elapsed);
                }
            }

            // Build Report
            var titleChart = "Dataflow compression with different degree of parallelism(dop)";
            var keys = fileSizesInGb.Select(s => $"{s}Gb").ToList();
            Charting.BuildReport(keys, degreesOfParallelism, results, titleChart);
        }

        public static void RunCompression(string srcFile, string dstFile, int degreeOfParallelism)
        {
            using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None,
                0x1000, true))
            using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.None, 0x1000, true))
            {
                CompressionAndEncryptDataFlow.CompressAndEncrypt(streamSource, streamDestination, degreeOfParallelism)
                    .Wait();
                streamDestination.Close();
            }
        }

        // 300 MB
        private static void CreateTextFileWithSize_300Mb()
        {
            CreateTextFileWithSize(300, Path.Combine(workDirectory, "txt_3Gb.text"));
        }

        // 3 GB
        private static void CreateTextFileWithSize_3Gb()
        {
            CreateTextFileWithSize(3 * 1024, Path.Combine(workDirectory, "txt_3Gb.text"));
        }

        // 6 GB
        private static void CreateTextFileWithSize_6GB()
        {
            CreateTextFileWithSize(6 * 1024, Path.Combine(workDirectory, "txt_6Gb.text"));
        }

        // 12 Gb
        private static void CreateTextFileWithSize_12GB()
        {
            CreateTextFileWithSize(12 * 1024, Path.Combine(workDirectory, "txt_12Gb.text"));
        }
    }
}