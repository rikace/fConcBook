using System;
using System.Threading;

namespace ThreadSafeRandom
{
    // Listing 13.5  Thread Safe Random number generator
    public sealed class ThreadSafeRandom : Random
    {
        private readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(MakeRandomSeed()));

        public override int Next()
        {
            return random.Value.Next();
        }

        public override int Next(int maxValue)
        {
            return random.Value.Next(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            return random.Value.Next(minValue, maxValue);
        }

        public override double NextDouble()
        {
            return random.Value.NextDouble();
        }

        public override void NextBytes(byte[] buffer)
        {
            random.Value.NextBytes(buffer);
        }

        // creates a seed that does not depend on the system-clock.
        // a unique value is created with each invocation
        private static int MakeRandomSeed()
        {
            return Guid.NewGuid().ToString().GetHashCode();
        }
    }
}