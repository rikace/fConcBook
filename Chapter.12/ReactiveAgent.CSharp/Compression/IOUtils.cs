using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ReactiveAgent.CSharp.ParallelCompression
{
    public static class IOUtils
    {
        private static readonly byte[] SALT =
            {0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c};

        private static readonly Lazy<Rijndael> rijndaelAlgorithm = new Lazy<Rijndael>(() =>
        {
            var rijndael = Rijndael.Create();
            var pdb = new Rfc2898DeriveBytes("buggghinabella", SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            return rijndael;
        });

        public static async Task<byte[]> Compress(byte[] data)
        {
            using (var memStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memStream, CompressionLevel.Optimal))
                {
                    await gzipStream.WriteAsync(data, 0, data.Length);
                }

                return memStream.ToArray();
            }
        }

        public static async Task<byte[]> Decompress(byte[] data)
        {
            using (var memStream = new MemoryStream())
            {
                using (var input = new MemoryStream(data))
                using (var gzipStream = new GZipStream(input, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(memStream);
                }

                return memStream.ToArray();
            }
        }

        public static string MD5FromBytes(byte[] data)
        {
            using (var md5 = MD5.Create())
            using (var stream = new MemoryStream(data))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }

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
            var size = args.Sum(l => l.Length);
            var data = new byte[size];
            var offSet = 0;
            for (var i = 0; i < args.Length; i++)
            {
                args[i].CopyTo(data, offSet);
                offSet += args[i].Length;
            }

            return data;
        }
    }
}