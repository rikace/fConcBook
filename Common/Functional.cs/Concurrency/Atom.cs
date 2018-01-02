using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrencyEx
{
    // Listing 3.3 Atom object to perform CAS instruction
    public class Atom<T> where T : class //#A
    {
        public Atom(T value)
        {
            this.value = value;
        }

        protected volatile T value;

        public T Value => value; //#B

        public virtual T Swap(Func<T, T> operation) //#C
        {
            T original, temp;
            do
            {
                original = value;
                temp = operation(original);
            }
            while (Interlocked.CompareExchange(ref value, temp, original) != original); //#D
            return original;
        }
    }

    public sealed class AtomOptimized<T> : Atom<T> where T : class
    {
        public AtomOptimized(T value) : base(value) { }

        public override T Swap(Func<T, T> operation)
        {
            T original, temp;
            original = value;
            temp = operation(original);
            if (Interlocked.CompareExchange(ref value, temp, original) != original)
            {
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce();
                    original = value;
                    temp = operation(original);
                }
                while (Interlocked.CompareExchange(ref value, temp, original) != original);
            }
            return original;
        }
    }
}
