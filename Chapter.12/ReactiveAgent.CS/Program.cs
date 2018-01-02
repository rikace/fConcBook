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

namespace ReactiveAgent.CS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WordCountAgentsExample.Run().Wait();

            Console.ReadLine();
        }
    }
}