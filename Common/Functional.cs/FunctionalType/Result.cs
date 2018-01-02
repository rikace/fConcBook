using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    using System.IO;
    using static ResultExtensions;

    //Listing 10.8 The generic Result<T> type in C#
    public struct Result<T>
    {
        public T Ok { get; }            // #A
        public Exception Error { get; } // #A

        public bool IsFailed { get => Error != null; }
        public bool IsOk => !IsFailed;

        public Result(T ok)     // #B
        {
            Ok = ok;
            Error = default(Exception);
        }
        public Result(Exception error)  // #B
        {
            Error = error;
            Ok = default(T);
        }

        public R Match<R>(Func<T, R> okMap, Func<Exception, R> failureMap)
              => IsOk ? okMap(Ok) : failureMap(Error);       // #C

        public void Match(Action<T> okAction, Action<Exception> errorAction)
        {
            if (IsOk) okAction(Ok); else errorAction(Error);  // #C
        }

        public static implicit operator Result<T>(T ok) => new Result<T>(ok);  // #D
        public static implicit operator Result<T>(Exception error) => new Result<T>(error); // #D

        public static implicit operator Result<T>(Result.Ok<T> ok) => new Result<T>(ok.Value); //#D
        public static implicit operator Result<T>(Result.Failure error)
              => new Result<T>(error.Error);        // #D
    }

    public static class Result
    {
        public struct Ok<L>
        {
            internal L Value { get; }
            internal Ok(L value) { Value = value; }

        }

        public struct Failure
        {
            internal Exception Error { get; }
            internal Failure(Exception error) { Error = error; }
        }
    }

    //Listing 10.10 Task<Result<T>> helper functions for compositional semantic
    public static class ResultExtensions
    {
        public static Result.Ok<T> Ok<T>(T value) => new Result.Ok<T>(value);
        public static Result.Failure Failure(Exception error) => new Result.Failure(error);

        public static async Task<Result<T>> TryCatch<T>(Func<Task<T>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public static Task<Result<T>> TryCatch<T>(Func<T> func) => TryCatch(() => Task.FromResult(func()));

        public static async Task<Result<R>> SelectMany<T, R>(this Task<Result<T>> resultTask, Func<T, Task<Result<R>>> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok);
        }

        public static async Task<Result<R>> Select<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }
        public static async Task<Result<R>> Match<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> actionOk, Func<Exception, Task<R>> actionError)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);
            if (result.IsFailed)
                return await actionError(result.Error);
            return await actionOk(result.Ok);
        }

        public static async Task<Result<T>> ToResult<T>(this Task<Option<T>> optionTask)
           where T : class
        {
            Option<T> opt = await optionTask.ConfigureAwait(false);

            if (opt.IsSome()) return Ok(opt.Value);
            return new Exception();
        }

        public static async Task<Result<R>> OnSuccess<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }

        public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Func<Task> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                await func().ConfigureAwait(false);
            return result;
        }

        public static async Task<Result<R>> Bind<T, R>(this Task<Result<T>> resultTask, Func<T, Task<Result<R>>> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;
            return await func(result.Ok).ConfigureAwait(false);
        }

        public static async Task<Result<R>> Map<T, R>(this Task<Result<T>> resultTask, Func<T, Task<R>> func)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return result.Error;

            return await TryCatch(() => func(result.Ok));
        }

        public static async Task<Result<R>> Match<T, R>(this Task<Result<T>> resultTask, Func<T, R> actionOk, Func<Exception, R> actionError)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed)
                return actionError(result.Error);
            return actionOk(result.Ok);
        }

        public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, Task<bool>> predicate, string errorMessage)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsFailed || !await predicate(result.Ok).ConfigureAwait(false)) return result.Error;
            return result.Ok;
        }

        public static async Task<Result<T>> Tap<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            if (result.IsOk)
                await action(result.Ok).ConfigureAwait(false);
            return result;
        }
    }
}