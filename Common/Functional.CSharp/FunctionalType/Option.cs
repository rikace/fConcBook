using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Functional.CSharp.FuctionalType.Option;
using Microsoft.FSharp.Core;

namespace Functional.CSharp.FuctionalType
{
    using static OptionHelpers;

    public struct Option<T> : IEquatable<None>, IEquatable<Option<T>>
    {
        public readonly T Value;
        private readonly bool isSome;
        private bool isNone => !isSome;

        private Option(T value)
        {
            if (value == null)
                throw new ArgumentNullException();
            isSome = true;
            Value = value;
        }

        public static implicit operator Option<T>(None _)
        {
            return new Option<T>();
        }

        public static implicit operator Option<T>(Some<T> some)
        {
            return new Option<T>(some.Value);
        }

        public static implicit operator Option<T>(T value)
        {
            return value == null ? None : Some(value);
        }

        public R Match<R>(Func<R> none, Func<T, R> some)
        {
            return isSome ? some(Value) : none();
        }

        public bool Equals(Option<T> other)
        {
            return isSome == other.isSome
                   && (isNone || Value.Equals(other.Value));
        }

        public bool Equals(None _)
        {
            return isNone;
        }

        public override int GetHashCode()
        {
            var hashCode = -496720002;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + isSome.GetHashCode();
            hashCode = hashCode * -1521134295 + isNone.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Option<T>)) return false;
            var opt = (Option<T>) obj;
            if (opt.isSome && isSome) return opt.Value.Equals(Value);
            if (opt.isNone && isNone) return true;
            return false;
        }

        public static bool operator ==(Option<T> @this, Option<T> other)
        {
            return @this.Equals(other);
        }

        public static bool operator !=(Option<T> @this, Option<T> other)
        {
            return !(@this == other);
        }
    }

    namespace Option
    {
        public struct None
        {
            internal static readonly None Default = new None();
        }

        public struct Some<T>
        {
            internal T Value { get; }

            internal Some(T value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                Value = value;
            }
        }
    }

    public static class OptionHelpers
    {
        public static None None => None.Default; // the None value

        public static Option<T> Some<T>(T value)
        {
            return new Some<T>(value); // wrap the given value into a Some
        }

        public static Option<T> ToOption<T>(FSharpOption<T> fsOption)
        {
            return FSharpOption<T>.get_IsSome(fsOption)
                ? Some(fsOption.Value)
                : None;
        }

        // Convert the Option into an F# Option
        public static FSharpOption<T> ToFsOption<T>(Option<T> option)
        {
            return option.Match(() => FSharpOption<T>.None,
                v => FSharpOption<T>.Some(v));
        }


        public static Option<R> Apply<T, R>
            (this Option<Func<T, R>> @this, Option<T> arg)
        {
            return @this.Match(
                () => None,
                func => arg.Match(
                    () => None,
                    val => Some(func(val))));
        }

        public static Option<R> Bind<T, R>
            (this Option<T> optT, Func<T, Option<R>> f)
        {
            return optT.Match(
                () => None,
                t => f(t));
        }

        public static Option<R> Map<T, R>
            (this None _, Func<T, R> f)
        {
            return None;
        }

        public static Option<R> Map<T, R>
            (this Some<T> some, Func<T, R> f)
        {
            return Some(f(some.Value));
        }

        public static Option<R> Map<T, R>
            (this Option<T> optT, Func<T, R> f)
        {
            return optT.Match(
                () => None,
                t => Some(f(t)));
        }

        public static Option<Func<T2, R>> Map<T1, T2, R>
            (this Option<T1> @this, Func<T1, T2, R> func)
        {
            return @this.Map(func.Curry());
        }

        public static Option<Func<T2, T3, R>> Map<T1, T2, T3, R>
            (this Option<T1> @this, Func<T1, T2, T3, R> func)
        {
            return @this.Map(func.CurryFirst());
        }

        public static Unit Match<T>(this Option<T> @this, Action None, Action<T> Some)
        {
            return @this.Match(None.ToFunc(), Some.ToFunc());
        }

        public static bool IsSome<T>(this Option<T> @this)
        {
            return @this.Match(
                () => false,
                _ => true);
        }

        internal static T ValueUnsafe<T>(this Option<T> @this)
        {
            return @this.Match(
                () => { throw new InvalidOperationException(); },
                t => t);
        }

        public static T GetOrElse<T>(this Option<T> opt, T defaultValue)
        {
            return opt.Match(
                () => defaultValue,
                t => t);
        }

        public static T GetOrElse<T>(this Option<T> opt, Func<T> fallback)
        {
            return opt.Match(
                () => fallback(),
                t => t);
        }

        public static Task<T> GetOrElse<T>(this Option<T> opt, Func<Task<T>> fallback)
        {
            return opt.Match(
                () => fallback(),
                t => Task.FromResult(t));
        }

        public static Option<T> OrElse<T>(this Option<T> left, Option<T> right)
        {
            return left.Match(
                () => right,
                _ => left);
        }

        public static Option<T> OrElse<T>(this Option<T> left, Func<Option<T>> right)
        {
            return left.Match(
                () => right(),
                _ => left);
        }

        public static Option<R> Select<T, R>(this Option<T> @this, Func<T, R> func)
        {
            return @this.Map(func);
        }

        public static Option<T> Where<T>
            (this Option<T> optT, Func<T, bool> predicate)
        {
            return optT.Match(
                () => None,
                t => predicate(t) ? optT : None);
        }

        public static Option<RR> SelectMany<T, R, RR>
            (this Option<T> opt, Func<T, Option<R>> bind, Func<T, R, RR> project)
        {
            return opt.Match(
                () => None,
                t => bind(t).Match(
                    () => None,
                    r => Some(project(t, r))));
        }
    }
}