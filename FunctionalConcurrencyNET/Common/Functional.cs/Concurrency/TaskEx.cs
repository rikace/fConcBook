using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.Tasks
{
    public static class TaskEx
    {
        public static Task<T2> Next<T1, T2>(this Task<T1> task, Func<T1, Task<T2>> next)
        {
            var tcs = new TaskCompletionSource<T2>();
            task.ContinueWith(cont =>
            {
                if (cont.IsFaulted) tcs.TrySetException(cont.Exception.InnerExceptions);
                else if (cont.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    next(cont.Result).ContinueWith(nextCont =>
                    {
                        if (nextCont.IsFaulted) tcs.TrySetException(nextCont.Exception.InnerExceptions);
                        else if (nextCont.IsCanceled) tcs.TrySetCanceled();
                        else tcs.TrySetResult(nextCont.Result);
                    });
                }
            });
            return tcs.Task;
        }

        public static Task<TResult> Match<TInput, TResult>(this Task<TInput> task, Func<TInput, TResult> success, Action cancel, Action<AggregateException> faillure)
        {
            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith(cont =>
                 {
                     if (cont.IsFaulted)
                     {
                         tcs.SetException(cont.Exception.InnerException);
                         faillure(cont.Exception);
                     }
                     else if (cont.IsCanceled)
                     {
                         tcs.SetCanceled();
                         cancel();
                     }
                     else tcs.SetResult(success(cont.Result));
                 });
            return tcs.Task;
        }

        //Listing 10.3 Task.Catch function

public static Task<T> Catch<T, TError>(this Task<T> task, Func<TError, T> onError) where TError : Exception
{
    var tcs = new TaskCompletionSource<T>();    // #A
    task.ContinueWith(innerTask =>
    {
        if (innerTask.IsFaulted && innerTask?.Exception?.InnerException is TError)
            tcs.SetResult(onError((TError)innerTask.Exception.InnerException)); // #B
        else if (innerTask.IsCanceled)
            tcs.SetCanceled();      // #B
        else if (innerTask.IsFaulted)
            tcs.SetException(innerTask?.Exception?.InnerException ?? throw new InvalidOperationException()); // #B
        else
            tcs.SetResult(innerTask.Result);  // #B
    });
    return tcs.Task;
}

        //public static Task<T> Otherwise<T>
        //   (this Task<T> task, Func<Task<T>> fallback)
        //   => task.ContinueWith(t =>
        //         t.Status == TaskStatus.Faulted
        //            ? fallback()
        //            : Task.FromResult(t.Result)
        //      )
        //      .Unwrap();

        public static Task<T2> Fmap<T1, T2>(this Task<T1> input, Func<T1, T2> f) => input.ContinueWith(t => f(t.Result));

        public static Task<T2> map<T1, T2>(this Func<T1, T2> f, Task<T1> input) => input.ContinueWith(t => f(t.Result));

        public static Task<T> Return<T>(this T input) => Task.FromResult(input);

        public static Task<a> Pure<a>(a input) => Task.FromResult(input);

        public static Task<R> Apply<T, R>(this Task<Func<T, R>> liftedFn, Task<T> task)
        {
            var tcs = new TaskCompletionSource<R>();
            liftedFn.ContinueWith(innerLiftTask =>
                task.ContinueWith(innerTask =>
                    tcs.SetResult(innerLiftTask.Result(innerTask.Result))
            ));
            return tcs.Task;
        }



        static   Result<byte[]> ReadFile(string path)
        {
            if (File.Exists(path)) return File.ReadAllBytes(path);
            else return new FileNotFoundException(path);
        }


        static async Task<R> ApplyNew<T, R>(this Task<Func<T, R>> f, Task<T> arg)
           => (await f.ConfigureAwait(false))(await arg.ConfigureAwait(false));



        static Func<T1, Func<T2, TR>> Curry<T1, T2, TR>(this Func<T1, T2, TR> func) => p1 => p2 => func(p1, p2);

        public static Task<Func<b, c>> Apply<a, b, c>(this Task<Func<a, b, c>> liftedFn, Task<a> input)
            => Apply(liftedFn.Fmap(Curry), input);



        //public static Task<b> Apply1<a, b>(this Task<a> input, Task<Func<a, b>> liftedFn)
        //{
        //    var tcs = new TaskCompletionSource<b>();
        //    liftedFn.ContinueWith(f =>
        //       input.ContinueWith(x =>
        //          tcs.SetResult(f.Result(x.Result))
        //   ));
        //    return tcs.Task;
        //}

        public static Task<T2> Bind<T1, T2>(this Task<T1> input, Func<T1, Task<T2>> f)
        {
            var tcs = new TaskCompletionSource<T2>();
            input.ContinueWith(x =>
                f(x.Result).ContinueWith(y =>
                    tcs.SetResult(y.Result)));
            return tcs.Task;
        }

        public static Task<T2> SelectMany<T1, T2>(this Task<T1> first, Func<T1, Task<T2>> next)
        {
            var tcs = new TaskCompletionSource<T2>();
            first.ContinueWith(delegate
            {
                if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
                else if (first.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var t = next(first.Result);
                        if (t == null) tcs.TrySetCanceled();
                        else t.ContinueWith(delegate
                        {
                            if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                            else if (t.IsCanceled) tcs.TrySetCanceled();
                            else tcs.TrySetResult(t.Result);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        /////<summary>Transforms a task's result, or propagates its exception or cancellation.</summary>
        //public static Task<TOut> Select<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> projection)
        //{
        //    var r = new TaskCompletionSource<TOut>();
        //    task.ContinueWith(self => {
        //        if (self.IsFaulted) r.SetException(self.Exception.InnerExceptions);
        //        else if (self.IsCanceled) r.SetCanceled();
        //        else r.SetResult(projection(self.Result));
        //    });
        //    return r.Task;
        //}
        /////<summary>Transforms a task's result and then does a combined transform, propagating any exceptions or cancellation.</summary>
        //public static Task<TOut> SelectMany<TIn, TMid, TOut>(
        //        this Task<TIn> task,
        //        Func<TIn, Task<TMid>> midProjection,
        //        Func<TIn, TMid, TOut> outProjection)
        //{
        //    return task.Select(inp => midProjection(inp).Select(mid => outProjection(inp, mid))).Unwrap();
        //}

        public static Task<T> ToTaskOf<T>(this Task t)
        {
            if (t is Task<T>) return (Task<T>)t;
            var tcs = new TaskCompletionSource<T>();
            t.ContinueWith(ant =>
            {
                if (ant.IsCanceled) tcs.SetCanceled();
                else if (ant.IsFaulted) tcs.SetException(ant.Exception.InnerException);
                else tcs.SetResult(default(T));
            });
            return tcs.Task;
        }

        public static Task<T3> SelectMany<T1, T2, T3>(
            this Task<T1> input, Func<T1, Task<T2>> f, Func<T1, T2, T3> projection)
        {
            return Bind(input, outer =>
                   Bind(f(outer), inner =>
                   Return(projection(outer, inner))));
        }

        //public static Task<T2> Next<T1, T2>(this Task<T1> first, Func<T1, Task<T2>> next)
        //{
        //    if (first == null) throw new ArgumentNullException("first");
        //    if (next == null) throw new ArgumentNullException("next");

        //    var tcs = new TaskCompletionSource<T2>();
        //    first.ContinueWith(delegate
        //    {
        //        if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
        //        else if (first.IsCanceled) tcs.TrySetCanceled();
        //        else
        //        {
        //            try
        //            {
        //                var t = next(first.Result);
        //                if (t == null) tcs.TrySetCanceled();
        //                else t.ContinueWith(delegate
        //                {
        //                    if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
        //                    else if (t.IsCanceled) tcs.TrySetCanceled();
        //                    else tcs.TrySetResult(t.Result);
        //                }, TaskContinuationOptions.ExecuteSynchronously);
        //            }
        //            catch (Exception exc) { tcs.TrySetException(exc); }
        //        }
        //    }, TaskContinuationOptions.ExecuteSynchronously);
        //    return tcs.Task;
        //}


        public static Task<TOut> Select<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> projection)
        {
            var r = new TaskCompletionSource<TOut>();
            task.ContinueWith(self =>
            {
                if (self.IsFaulted) r.SetException(self.Exception.InnerExceptions);
                else if (self.IsCanceled) r.SetCanceled();
                else r.SetResult(projection(self.Result));
            });
            return r.Task;
        }

        public static Task<T> Where<T>(this Task<T> task, Func<T, bool> predicate)
        {
            var r = new TaskCompletionSource<T>();
            task.ContinueWith(self =>
            {
                if (self.IsFaulted) r.SetException(self.Exception.InnerExceptions);
                else if (self.IsCanceled) r.SetCanceled();
                else
                {
                    if (!predicate(self.Result)) throw new OperationCanceledException();
                    r.SetResult(self.Result);
                }
            });
            return r.Task;
        }

        public static Task<IEnumerable<T>> Traverese<T>(this IEnumerable<Task<T>> sequence)
        {
            return sequence.Aggregate(
                Task.FromResult(Enumerable.Empty<T>()),
                (eventualAccumulator, eventualItem) =>
                    from accumulator in eventualAccumulator
                    from item in eventualItem
                    select accumulator.Concat(new[] { item }).ToArray().AsEnumerable());
        }


        public static IEnumerable<Task<T>> ProcessAsComplete<T>(this IEnumerable<Task<T>> inputTasks)
        {
            // Copy the input so we know it’ll be stable, and we don’t evaluate it twice
            var inputTaskList = inputTasks.ToList();
            // Could use Enumerable.Range here, if we wanted…
            var completionSourceList = new List<TaskCompletionSource<T>>(inputTaskList.Count);
            for (int i = 0; i < inputTaskList.Count; i++)
            {
                completionSourceList.Add(new TaskCompletionSource<T>());
            }

            // At any one time, this is "the index of the box we’ve just filled".
            // It would be nice to make it nextIndex and start with 0, but Interlocked.Increment
            // returns the incremented value…
            int prevIndex = -1;

            // We don’t have to create this outside the loop, but it makes it clearer
            // that the continuation is the same for all tasks.
            Action<Task<T>> continuation = completedTask =>
            {
                int index = Interlocked.Increment(ref prevIndex);
                var source = completionSourceList[index];
                switch (completedTask.Status)
                {
                    case TaskStatus.Canceled:
                        source.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        source.TrySetException(completedTask.Exception.InnerExceptions);
                        break;
                    case TaskStatus.RanToCompletion:
                        source.TrySetResult(completedTask.Result);
                        break;
                    default:
                        // TODO: Work out whether this is really appropriate. Could set
                        // an exception in the completion source, of course…
                        throw new ArgumentException("Task was not completed");
                }
            };

            foreach (var inputTask in inputTaskList)
            {
                inputTask.ContinueWith(continuation,
                                       CancellationToken.None,
                                       TaskContinuationOptions.ExecuteSynchronously,
                                       TaskScheduler.Default);
            }

            return completionSourceList.Select(source => source.Task);
        }


        //Listing 10.24  C# asynchronous lift functions
        public static Task<R> Lift2<T1, T2, R>(Func<T1, T2, R> selector, Task<T1> item1, Task<T2> item2) // #A
        {
            Func<T1, Func<T2, R>> curry = x => y => selector(x, y);    // #B
            var lifted1 = Pure(curry);              // #C
            var lifted2 = Apply(lifted1, item1);    // #D
            return Apply(lifted2, item2);           // #D
        }

        public static Task<R> Lift3<T1, T2, T3, R>(Func<T1, T2, T3, R> selector, Task<T1> item1, Task<T2> item2, Task<T3> item3)    // #A
        {
            Func<T1, Func<T2, Func<T3, R>>> curry = x => y => z => selector(x, y, z); // #B
            var lifted1 = Pure(curry);              // #C
            var lifted2 = Apply(lifted1, item1);    // #D
            var lifted3 = Apply(lifted2, item2);    // #D
            return Apply(lifted3, item3);           // #D
        }

    }
}


