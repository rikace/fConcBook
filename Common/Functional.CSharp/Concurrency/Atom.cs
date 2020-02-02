using System;
using System.Threading;

namespace Functional.CSharp.Concurrency
{
    // Listing 3.3 Atom object to perform CAS instruction
    public class Atom<T> where T : class //#A
    {
        protected volatile T value;

        public Atom(T value)
        {
            this.value = value;
        }

        public T Value => value; //#B

        public virtual T Swap(Func<T, T> operation) //#C
        {
            T original, temp;
            do
            {
                original = value;
                temp = operation(original);
            }
#pragma warning disable 420
            while (Interlocked.CompareExchange(ref value, temp, original) != original); //#D
#pragma warning restore 420
            return original;
        }
    }

    public sealed class AtomOptimized<T> : Atom<T> where T : class
    {
        public AtomOptimized(T value) : base(value)
        {
        }

        public override T Swap(Func<T, T> operation)
        {
            var original = value;
            var temp = operation(original);
#pragma warning disable 420
            if (Interlocked.CompareExchange(ref value, temp, original) != original)
#pragma warning restore 420
            {
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce();
                    original = value;
                    temp = operation(original);
                }
#pragma warning disable 420
                while (Interlocked.CompareExchange(ref value, temp, original) != original);
#pragma warning restore 420
            }

            return original;
        }
    }
}