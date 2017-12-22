using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PersistentDataStructures
{
    // Listing 3.3 Atom object to perform CAS instruction
    public sealed class Atom<T> where T : class //#A
    {
        public Atom(T value)
        {
            this.value = value;
        }

        private volatile T value;

        public T Value => value; //#B

        public T Swap(Func<T, T> operation) //#C
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

    public sealed class AtomOptimized<T> where T : class
    {
        public AtomOptimized(T value)
        {
            this.value = value;
        }

        private volatile T value;

        public T Value => value;

        public T Swap(Func<T, T> operation)
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
