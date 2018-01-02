using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Combinators.cs
{
    class Log
    {
        internal static Task Error(Exception ex)
        {
            throw new NotImplementedException();
        }

        internal static void Error(string v, Exception ex)
        {
        }
    }
}
