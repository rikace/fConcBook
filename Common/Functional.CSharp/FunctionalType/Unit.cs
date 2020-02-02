using System;

namespace Functional.CSharp.FuctionalType
{
    // Listing 7.3 Unit type implementation in C#
    public struct Unit : IEquatable<Unit> // #A
    {
        public static readonly Unit Default = new Unit(); // #B

        public override int GetHashCode()
        {
            return 0; // #C
        }

        public override bool Equals(object obj)
        {
            return obj is Unit; // #C
        }

        public override string ToString()
        {
            return "()";
        }

        public bool Equals(Unit other)
        {
            return true; // #D
        }

        public static bool operator ==(Unit lhs, Unit rhs)
        {
            return true; // #D
        }

        public static bool operator !=(Unit lhs, Unit rhs)
        {
            return false; // #D
        }
    }
}