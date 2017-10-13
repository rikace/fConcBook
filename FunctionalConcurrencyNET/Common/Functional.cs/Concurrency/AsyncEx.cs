using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.Async
{
    public static class AsyncEx
    {
        public static Task<T> Return<T>(T task) => Task.FromResult(task);

        public static async Task<R> Bind<T, R>(this Task<T> task, Func<T, Task<R>> cont)
            => await cont(await task.ConfigureAwait(false)).ConfigureAwait(false);

        public static async Task<R> Map<T, R>(this Task<T> task, Func<T, R> map)
            => map(await task.ConfigureAwait(false));


        public static async Task<R> SelectMany<T, R>(this Task<T> task,
            Func<T, Task<R>> then) => await Bind(task, then);

        public static async Task<R> SelectMany<T1, T2, R>(this Task<T1> task,
            Func<T1, Task<T2>> bind, Func<T1, T2, R> project)
        {
            T1 taskResult = await task;
            return project(taskResult, await bind(taskResult));
        }

        public static async Task<R> Select<T, R>(this Task<T> task, Func<T, R> project)
            => await Map(task, project);

        //Listing 10.2 Refresh of the Otherwise and Retry function
        public static Task<T> Otherwise<T>(this Task<T> task, Func<Task<T>> orTask) =>	// #A
            task.ContinueWith(async innerTask =>
            {
                if (innerTask.Status == TaskStatus.Faulted) return await orTask();
                return await Task.FromResult<T>(innerTask.Result);
            }).Unwrap();


public static async Task<T> Retry<T>(Func<Task<T>> task, int retries,     // #B
                        TimeSpan delay, CancellationToken cts = default(CancellationToken)) =>
    await task().ContinueWith(async innerTask =>
    {
        cts.ThrowIfCancellationRequested();
        if (innerTask.Status != TaskStatus.Faulted)
            return innerTask.Result;
        if (retries == 0)
            throw innerTask.Exception ?? throw new Exception();
        await Task.Delay(delay, cts);
        return await Retry(task, retries - 1, delay, cts);
    }).Unwrap();


        public static async Task<T> Tap<T>(this Task<T> task, Func<Task<T>, Task> operation) // #A
        {
            await operation(task);
            return await task;
        }

        public static async Task<T> Tap<T>(this Task<T> task, Func<T, Task> action)
        {
            await action(await task);
            return await task;
        }

    }
}
