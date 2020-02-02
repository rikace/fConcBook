using System;

namespace ReactiveAgent.CSharp.ParallelCompression
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

    internal struct CompressingDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] ChunkSize { get; set; }
    }

    internal struct CompressedDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] ChunkSize { get; set; }
        public byte[] CompressedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }

    internal struct EncryptDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] EncryptedDataSize { get; set; }
        public bool IsProcessed { get; set; }
    }

    internal struct DecompressionDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public long ChunkSize { get; set; }
        public bool IsProcessed { get; set; }
    }

    internal struct DecryptDetails
    {
        public byte[] Bytes { get; set; }
        public int Sequence { get; set; }
        public byte[] EncryptedDataSize { get; set; }
    }
}