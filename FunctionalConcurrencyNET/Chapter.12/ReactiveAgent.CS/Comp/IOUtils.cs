using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveAgent.CS
{
    public static class IOUtils
    {
        public static async Task<byte[]> Compress(byte[] data)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memStream, CompressionLevel.Optimal))
                {
                    await gzipStream.WriteAsync(data, 0, data.Length);
                }
                return memStream.ToArray();
            }
        }

        public static async Task<byte[]> Decompress(byte[] data)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (MemoryStream input = new MemoryStream(data))
                using (GZipStream gzipStream = new GZipStream(input, CompressionMode.Decompress))
                    await gzipStream.CopyToAsync(memStream);
                return memStream.ToArray();
            }
        }

        public static string MD5FromBytes(byte[] data)
        {
            using (var md5 = MD5.Create())
            using (var stream = new MemoryStream(data))
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        }
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        private static Lazy<Rijndael> rijndaelAlgorithm = new Lazy<Rijndael>(() =>
        {
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("buggghinabella", SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            return rijndael;
        });

        public static Task<byte[]> Encrypt(byte[] data)
        {
            return CryptoTransform(data, rijndaelAlgorithm.Value.CreateEncryptor());
        }
        public static Task<byte[]> Decrypt(byte[] data)
        {
            return CryptoTransform(data, rijndaelAlgorithm.Value.CreateDecryptor());
        }

        public static async Task<byte[]> CryptoTransform(byte[] data, ICryptoTransform transform)
        {
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }

        internal static byte[] CombineByteArrays(params byte[][] args)
        {
            int size = args.Sum(l => l.Length);
            byte[] data = new byte[size];
            int offSet = 0;
            for (int i = 0; i < args.Length; i++)
            {
                args[i].CopyTo(data, offSet);
                offSet += args[i].Length;
            }
            return data;
        }
    }
}