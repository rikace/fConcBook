using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Serializer
    {
        public static byte[] Serialize(IFormatter formatter, object data)
        {
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, data);
                var msg = ms.ToArray();
                var size = BitConverter.GetBytes(msg.Length);
                return size.Concat(msg).ToArray();
            }
        }

        public static T Deserialize<T>(IFormatter formatter, ArraySegment<byte> data) where T : class
        {   using (var ms = new MemoryStream(data.Array, data.Offset, data.Count))
                return formatter.Deserialize(ms) as T;
        }
    }
}
