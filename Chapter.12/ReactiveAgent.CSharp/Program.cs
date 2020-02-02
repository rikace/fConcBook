using System;
using System.Threading.Tasks;
using ReactiveAgent.CSharp.Agents;

namespace ReactiveAgent.CSharp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await WordCountAgentsExample.Run();

            Console.ReadLine();
        }
    }
}