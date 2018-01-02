using FSharp.Charting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataflowObjectPoolEncryption
{
    class Program
    {
        static void Main(string[] args)
        {
            RunGcComparison(new[] { 1, 2, 3 });
            Console.ReadLine();
        }

        private static string workDirectory = @".\Data";
        private static string templateFile = @".\Data\Ulysses.txt";

        public static void CompressAndEncrypt(string srcFile, string dstFile, string rstFile)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                Console.WriteLine("CompressAndEncrypt ...");
                var dataflow = new CompressionAndEncryptDataFlow(Environment.ProcessorCount);

                if (File.Exists(dstFile))
                    File.Delete(dstFile);

                using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
                using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
                {
                    dataflow.CompressAndEncrypt(streamSource, streamDestination).Wait();
                    streamDestination.Close();
                }

                Console.WriteLine($"Done in {sw.ElapsedMilliseconds} with {dataflow.Pool.Size} allocated chunks");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
                sw.Restart();

                if (File.Exists(rstFile))
                    File.Delete(rstFile);

                Console.WriteLine("DecryptAndDecompress ...");
                using (var streamSource = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
                using (var streamDestination = new FileStream(rstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
                    dataflow.DecryptAndDecompress(streamSource, streamDestination).Wait();

                Console.WriteLine($"Done in {sw.ElapsedMilliseconds}  with {dataflow.Pool.Size} allocated chunks");

                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();

                Console.WriteLine("Verification ...");
                using (var f1 = File.OpenRead(srcFile))
                using (var f2 = File.OpenRead(rstFile))
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

        public static void CreateTextFileWithSize(string path, string templateFilePath, long targetSize)
        {
            var bytes = File.ReadAllBytes(templateFilePath);

            using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write))
            {
                var iterations = (targetSize - fs.Length + bytes.Length) / bytes.Length;

                for (var i = 0; i < iterations; i++)
                    fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static void RunGcComparison(int[] fileSizesInGb)
        {
            const long bytesInGb = 1024L * 1024 * 1024;
            string inFile = Path.Combine(workDirectory, "inFile.txt");
            string outFile = Path.Combine(workDirectory, "outFile.txt");

            var results = new List<List<PerfUtil.PerfResult>>()
            {
                new List<PerfUtil.PerfResult>(),
                new List<PerfUtil.PerfResult>()
            };
            var titles = new List<List<string>>()
            {
                new List<string>(),
                new List<string>()
            };

            (Action<string, string>,string)[] methods = new (Action<string, string>,string)[2];
            methods[0] = (RunCompression, "without ObjectPool");
            methods[1] = (RunCompressionObjPool, "with ObjectPool");

            foreach (var size in fileSizesInGb)
            {
                Console.WriteLine($"Creating input file {size}GB ...");
                if (File.Exists(inFile))
                    File.Delete(inFile);
                CreateTextFileWithSize(inFile, templateFile, bytesInGb * size);

                for (var i=0; i<methods.Length; i++)
                {
                    var (func, name) = methods[i];
                    if (File.Exists(outFile))
                        File.Delete(outFile);
                    Console.WriteLine("GC.Collect() ...");
                    GC.Collect();
                    GC.Collect();
                    Console.WriteLine($"Running compression ...");
                    var res = PerfVis.Run(() => func(inFile, outFile));
                    Console.WriteLine($"\t Elapsed = {res.Elapsed}");

                    results[i].Add(res);
                    titles[i].Add($"{name} with {size}Gb");
                }
            }

            results[0].AddRange(results[1]);
            titles[0].AddRange(titles[1]);
            Application.Run(PerfVis.CombineAndShowGcCharts(titles[0].ToArray(), results[0].ToArray()));
        }

        public static void RunCompressionObjPool(string srcFile, string dstFile)
        {
            var dataflow = new CompressionAndEncryptDataFlow(Environment.ProcessorCount);
            using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
            using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
            {
                dataflow.CompressAndEncrypt(streamSource, streamDestination).Wait();
                streamDestination.Close();
            }
        }

        public static void RunCompression(string srcFile, string dstFile)
        {
            using (var streamSource = new FileStream(srcFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 0x1000, useAsync: true))
            using (var streamDestination = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 0x1000, useAsync: true))
            {
                ParallelCompressionCS.CompressionAndEncryptDataFlow.CompressAndEncrypt(streamSource, streamDestination, Environment.ProcessorCount).Wait();
                streamDestination.Close();
            }
        }
    }
}
