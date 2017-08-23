using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.IO
{
    public static class File
    {
       public static Task WriteAllTextAsync(string path, string content)
        {
            return Task.FromResult(true);
        }

        public static Task<string[]> ReadAllLinesAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}
