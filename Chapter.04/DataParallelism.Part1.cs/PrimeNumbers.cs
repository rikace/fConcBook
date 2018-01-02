using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataParallelism.cs
{
    public static class PrimeNumbers
    {
        public static long PrimeSumSequential()
        {
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            for (var i=1; i<=len; ++i)
            {
                if (isPrime(i))
                    total += i;
            }

            return total;
        }

        public static long PrimeSumParallel()
        {
            // Listing 4.4 Parallel sum of prime numbers in a collection using Parallel.For loop construct
            int len = 10000000;
            long total = 0;                  //#A
            Func<int, bool> isPrime = n =>  //#B
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int) Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n%i == 0) return false;
                return true;
            };

            Parallel.For(0, len, i => //#C
            {
                if (isPrime(i))       //#D
                    total += i;       //#D
            });
            return total;
        }

        public static long PrimeSumParallelThreadLocal()
        {
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // Listing 4.5 Thread-safe parallel sum using Parallel.For and ThreadLocal
            Parallel.For(0, len,
                () => 0,        //#A
                (int i, ParallelLoopState loopState, long tlsValue)  //#B
                    => isPrime(i) ? tlsValue += i : tlsValue,
                value => Interlocked.Add(ref total, value));        //#C

            return total;
        }

        public static long PrimeSumParallelLINQ()
        {
            int len = 10000000;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // Listing 4.7 Parallel sum of a collection using declarative PLINQ
            long total = Enumerable.Range(0, len).AsParallel().Where(isPrime).Sum(x => (long)x); //#B
            return total;
        }
    }
}