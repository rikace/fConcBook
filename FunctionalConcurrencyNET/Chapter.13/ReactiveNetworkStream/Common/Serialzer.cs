using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Serialzer
    {
        public static byte[] Serialize(IFormatter formatter, object data)
        {
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, data);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(IFormatter formatter, byte[] data) where T : class
        {   using (var ms = new MemoryStream(data))
                return formatter.Deserialize(ms) as T;
        }
    }
}
