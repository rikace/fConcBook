using System;
using System.Threading;
using System.Threading.Tasks;
using GraphTasks.FSharp;

namespace GraphTasks.CSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Func<int, int, Func<Task>> action = (id, delay) => async () =>
            {
                Console.WriteLine($"Starting operation{id} in Thread Id {Thread.CurrentThread.ManagedThreadId}...");
                await Task.Delay(delay);
            };

            var dagAsync = new DirectGraphDependencies.ParallelTasksDAG();

            dagAsync
                .OnTaskCompleted.Subscribe(op =>
                    Console.WriteLine(
                        $"Operation {op.Id} completed in Thread Id {Thread.CurrentThread.ManagedThreadId}"));

            dagAsync.AddTask(1, action(1, 600), 4, 5);
            dagAsync.AddTask(2, action(2, 200), 5);
            dagAsync.AddTask(3, action(3, 800), 6, 5);
            dagAsync.AddTask(4, action(4, 500), 6);
            dagAsync.AddTask(5, action(5, 450), 7, 8);
            dagAsync.AddTask(6, action(6, 100), 7);
            dagAsync.AddTask(7, action(7, 900));
            dagAsync.AddTask(8, action(8, 700));

            dagAsync.ExecuteTasks();

            Console.ReadLine();
        }
    }
}