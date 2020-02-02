using System;

namespace FunctionalTechniques.CSharp
{
    internal static class Composition
    {
        // Listing 2.3 Compose function in C#
        private static Func<A, C> Compose<A, B, C>(this Func<A, B> f, Func<B, C> g)
        {
            return n => g(f(n));
        }
    }
}