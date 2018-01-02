using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataflowObjectPoolEncryption
{
    public class ChunkBytes
    {
        public ChunkBytes(int chunkSize)
        {
            Bytes = new byte[chunkSize];
        }

        public ChunkBytes(byte[] bytes)
        {
            Bytes = bytes;
            Length = bytes.Length;
        }

        public byte[] Bytes { get; }
        public int Length { get; set; }
    }
}