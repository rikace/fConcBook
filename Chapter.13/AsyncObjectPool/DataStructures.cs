using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataflowObjectPoolEncryption
{
    public struct CompressingDetails
    {
        public ChunkBytes Bytes { get; set; }
        public int Sequence { get; set; }
        public ChunkBytes ChunkSize { get; set; }
    }

    public struct CompressedDetails
    {
        public ChunkBytes Bytes { get; set; }
        public int Sequence { get; set; }
        public ChunkBytes ChunkSize { get; set; }
        public ChunkBytes CompressedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }
    public struct EncryptDetails
    {
        public ChunkBytes Bytes { get; set; }
        public int Sequence { get; set; }
        public ChunkBytes EncryptedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }

    public struct DecompressionDetails
    {

        public ChunkBytes Bytes { get; set; }
        public int Sequence { get; set; }
        public long ChunkSize { get; set; }
        public bool IsProcessed { get; set; }
    }
    public struct DecryptDetails
    {
        public ChunkBytes Bytes { get; set; }
        public int Sequence { get; set; }
        public ChunkBytes EncryptedDataSize { get; set; }
    }
}
