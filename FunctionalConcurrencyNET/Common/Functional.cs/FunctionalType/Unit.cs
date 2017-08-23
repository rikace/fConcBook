using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    // Listing 7.3 Unit type implementation in C#
    public struct Unit : IEquatable<Unit>  // #A
    {
        public static readonly Unit Default = new Unit();  // #B

        public override int GetHashCode() => 0;		// #C
        public override bool Equals(object obj) => obj is Unit;  // #C

        public override string ToString() => "()";

        public bool Equals(Unit other) => true;		// #D
        public static bool operator ==(Unit lhs, Unit rhs) => true; // #D
        public static bool operator !=(Unit lhs, Unit rhs) => false; // #D
    }
}
