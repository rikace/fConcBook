using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrencyEx
{
    public static class Atom
    {
        public static T SwapWithCas<T>(T value, Func<T, T> update) where T : class
        {
            T original, updated;
            do
            {
                original = value;
                updated = update(original);
            }
            while (original !=
               Interlocked.CompareExchange(ref value, updated, original));

            return updated;
        }
    }
}
