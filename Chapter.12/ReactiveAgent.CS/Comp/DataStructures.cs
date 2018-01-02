using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveAgent.CS
{
    public static class StructHelpers
    {
        internal static CompressedDetails ToCompressedDetails(this CompressingDetails details, byte[] compressedData)
        {
            return new CompressedDetails
            {
                Bytes = compressedData,
                ChunkSize = details.ChunkSize,
                Sequence = details.Sequence,
                CompressedDataSize = BitConverter.GetBytes(compressedData.Length)
            };
        }

        internal static EncryptDetails ToEncryptDetails(this CompressedDetails details, byte[] encryptedData)
        {
            return new EncryptDetails
            {
                Bytes = encryptedData,
                Sequence = details.Sequence,
                EncryptedDataSize = BitConverter.GetBytes(encryptedData.Length)
            };
        }
    }

    struct CompressingDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] ChunkSize { get; set; }
    }

    struct CompressedDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] ChunkSize { get; set; }
        public byte[] CompressedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }
    struct EncryptDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] EncryptedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }

    struct DecompressionDetails
    {

        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public long ChunkSize { get; set; }
        public bool IsProcessed { get; set; }
    }
    struct DecryptDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] EncryptedDataSize { get; set; }
    }
}
