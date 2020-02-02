using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ReactiveAgent.CSharp;

namespace DataflowObjectPoolEncryption
{
    public class CompressionAndEncryptDataFlow
    {
        private readonly int _boundedCapacity;

        private readonly int _chunkSize;
        private readonly int _maxDegreeOfParallelism;

        public CompressionAndEncryptDataFlow(int maxDegreeOfParallelism, int chunkSize = 1048576,
            int boundedCapacity = 20)
        {
            _chunkSize = chunkSize;
            _boundedCapacity = boundedCapacity;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            Pool = new ObjectPoolAsync<ChunkBytes>(0, () => new ChunkBytes(chunkSize * 3 / 2), CancellationToken.None);
        }

        public async Task CompressAndEncrypt(
            Stream streamSource, Stream streamDestination,
            CancellationTokenSource cts = null)
        {
            cts = cts ?? new CancellationTokenSource();

            var compressorOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = _boundedCapacity,
                CancellationToken = cts.Token
            };

            var inputBuffer = new BufferBlock<CompressingDetails>(
                new DataflowBlockOptions
                {
                    CancellationToken = cts.Token,
                    BoundedCapacity = _boundedCapacity
                });

            var compressor = new TransformBlock<CompressingDetails, CompressedDetails>(
                async details =>
                {
                    var compressedData = await Compress(details.Bytes);
                    await Pool.PutAsync(details.Bytes);

                    return new CompressedDetails
                    {
                        Bytes = compressedData,
                        ChunkSize = details.ChunkSize,
                        Sequence = details.Sequence,
                        CompressedDataSize = new ChunkBytes(BitConverter.GetBytes(compressedData.Length))
                    };
                }, compressorOptions);

            var encryptor = new TransformBlock<CompressedDetails, EncryptDetails>(
                async details =>
                {
                    var data = await CombineByteArrays(details.CompressedDataSize, details.ChunkSize, details.Bytes);
                    await Pool.PutAsync(details.Bytes);

                    var encryptedData = await Encrypt(data);
                    await Pool.PutAsync(data);

                    return new EncryptDetails
                    {
                        Bytes = encryptedData,
                        Sequence = details.Sequence,
                        EncryptedDataSize = new ChunkBytes(BitConverter.GetBytes(encryptedData.Length))
                    };
                }, compressorOptions);

            var asOrderedAgent = Agent.Start((new Dictionary<int, EncryptDetails>(), 0),
                async ((Dictionary<int, EncryptDetails>, int) state, EncryptDetails msg) =>
                {
                    var (details, lastIndexProc) = state;
                    details.Add(msg.Sequence, msg);
                    while (details.ContainsKey(lastIndexProc + 1))
                    {
                        msg = details[lastIndexProc + 1];
                        await streamDestination.WriteAsync(msg.EncryptedDataSize.Bytes, 0,
                            msg.EncryptedDataSize.Length);
                        await streamDestination.WriteAsync(msg.Bytes.Bytes, 0, msg.Bytes.Length);
                        await Pool.PutAsync(msg.Bytes);
                        lastIndexProc = msg.Sequence;
                        details.Remove(lastIndexProc);
                    }

                    return (details, lastIndexProc);
                }, cts);

            var writer = new ActionBlock<EncryptDetails>(async details => await
                asOrderedAgent.Send(details), compressorOptions);

            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            inputBuffer.LinkTo(compressor, linkOptions);
            compressor.LinkTo(encryptor, linkOptions);
            encryptor.LinkTo(writer, linkOptions);

            var sourceLength = streamSource.Length;
            var size = BitConverter.GetBytes(sourceLength);
            await streamDestination.WriteAsync(size, 0, size.Length);


            var chunkSize = _chunkSize < sourceLength ? _chunkSize : (int) sourceLength;
            var indexSequence = 0;
            while (sourceLength > 0)
            {
                var bytes = await ReadFromStream(streamSource, chunkSize);
                var compressingDetails = new CompressingDetails
                {
                    Bytes = bytes,
                    ChunkSize = new ChunkBytes(BitConverter.GetBytes(bytes.Length)),
                    Sequence = ++indexSequence
                };
                await inputBuffer.SendAsync(compressingDetails);
                sourceLength -= bytes.Length;
                if (sourceLength < chunkSize)
                    chunkSize = (int) sourceLength;
                if (sourceLength == 0)
                    inputBuffer.Complete();
            }

            await inputBuffer.Completion.ContinueWith(task => compressor.Complete());
            await compressor.Completion.ContinueWith(task => encryptor.Complete());
            await encryptor.Completion.ContinueWith(task => writer.Complete());
            await writer.Completion;
            await streamDestination.FlushAsync();
        }

        public async Task DecryptAndDecompress(
            Stream streamSource, Stream streamDestination,
            CancellationTokenSource cts = null)
        {
            cts = cts ?? new CancellationTokenSource();

            var compressorOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cts.Token,
                BoundedCapacity = _boundedCapacity
            };

            var inputBuffer = new BufferBlock<DecryptDetails>(
                new DataflowBlockOptions
                {
                    CancellationToken = cts.Token,
                    BoundedCapacity = _boundedCapacity
                });

            var decryptor = new TransformBlock<DecryptDetails, DecompressionDetails>(
                async details =>
                {
                    var decryptedData = await Decrypt(details.Bytes);
                    await Pool.PutAsync(details.Bytes);

                    return new DecompressionDetails
                    {
                        Bytes = decryptedData,
                        Sequence = details.Sequence
                    };
                }, compressorOptions);

            var decompressor = new TransformBlock<DecompressionDetails, DecompressionDetails>(
                async details =>
                {
                    var decompressedData = await Decompress(details.Bytes, sizeof(int) + sizeof(int));
                    await Pool.PutAsync(details.Bytes);

                    return new DecompressionDetails
                    {
                        Bytes = decompressedData,
                        Sequence = details.Sequence
                    };
                }, compressorOptions);

            var asOrderedAgent = Agent.Start((new Dictionary<int, DecompressionDetails>(), 0),
                async ((Dictionary<int, DecompressionDetails>, int) state, DecompressionDetails msg) =>
                {
                    //#G
                    var (details, lastIndexProc) = state;
                    details.Add(msg.Sequence, msg);
                    while (details.ContainsKey(lastIndexProc + 1))
                    {
                        msg = details[lastIndexProc + 1];
                        await streamDestination.WriteAsync(msg.Bytes.Bytes, 0, msg.Bytes.Length);
                        await Pool.PutAsync(msg.Bytes);
                        lastIndexProc = msg.Sequence;
                        details.Remove(lastIndexProc);
                    }

                    return (details, lastIndexProc);
                }, cts);

            var writer = new ActionBlock<DecompressionDetails>(async details =>
                await asOrderedAgent.Send(details), compressorOptions);

            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            inputBuffer.LinkTo(decryptor, linkOptions);
            decryptor.LinkTo(decompressor, linkOptions);
            decompressor.LinkTo(writer, linkOptions);

            GCSettings.LargeObjectHeapCompactionMode =
                GCLargeObjectHeapCompactionMode.CompactOnce;

            var size = new byte[sizeof(long)];
            await streamSource.ReadAsync(size, 0, size.Length);
            // convert the size to number
            var destinationLength = BitConverter.ToInt64(size, 0);
            streamDestination.SetLength(destinationLength);
            var sourceLength = streamSource.Length - sizeof(long);

            var index = 0;
            while (sourceLength > 0)
            {
                // read the encrypted chunk size
                size = new byte[sizeof(int)];
                var sizeReadCount = await streamSource.ReadAsync(size, 0, size.Length);

                // convert the size back to number
                var storedSize = BitConverter.ToInt32(size, 0);

                var encryptedData = await ReadFromStream(streamSource, storedSize);

                var decryptDetails = new DecryptDetails
                {
                    Bytes = encryptedData,
                    EncryptedDataSize = new ChunkBytes(size),
                    Sequence = ++index
                };

                await inputBuffer.SendAsync(decryptDetails);

                sourceLength -= sizeReadCount + encryptedData.Length;
                if (sourceLength == 0)
                    inputBuffer.Complete();
            }

            await inputBuffer.Completion.ContinueWith(task => decryptor.Complete());
            await decryptor.Completion.ContinueWith(task => decompressor.Complete());
            await decompressor.Completion.ContinueWith(task => writer.Complete());
            await writer.Completion;

            await streamDestination.FlushAsync();
        }

        #region IOUtils

        public readonly ObjectPoolAsync<ChunkBytes> Pool;

        private async Task<ChunkBytes> Compress(ChunkBytes data)
        {
            var result = await Pool.GetAsync();
            using (var memStream = new MemoryStream(result.Bytes))
            {
                using (var gzipStream = new GZipStream(memStream, CompressionLevel.Optimal, true))
                {
                    await gzipStream.WriteAsync(data.Bytes, 0, data.Length);
                }

                result.Length = (int) memStream.Position;
            }

            return result;
        }

        private async Task<ChunkBytes> Decompress(ChunkBytes data, int offset = 0)
        {
            var result = await Pool.GetAsync();
            using (var memStream = new MemoryStream(result.Bytes))
            {
                using (var input = new MemoryStream(data.Bytes, offset, data.Length - offset))
                using (var gzipStream = new GZipStream(input, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(memStream);
                }

                result.Length = (int) memStream.Position;
            }

            return result;
        }

        private static string MD5FromBytes(ChunkBytes data)
        {
            using (var md5 = MD5.Create())
            using (var stream = new MemoryStream(data.Bytes, 0, data.Length))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }

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

        private Task<ChunkBytes> Encrypt(ChunkBytes data)
        {
            return CryptoTransform(data, rijndaelAlgorithm.Value.CreateEncryptor());
        }

        private Task<ChunkBytes> Decrypt(ChunkBytes data)
        {
            return CryptoTransform(data, rijndaelAlgorithm.Value.CreateDecryptor());
        }

        private async Task<ChunkBytes> CryptoTransform(ChunkBytes data, ICryptoTransform transform)
        {
            var result = await Pool.GetAsync();
            using (var memoryStream = new MemoryStream(result.Bytes))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(data.Bytes, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    result.Length = (int) memoryStream.Position;
                }
            }

            return result;
        }

        private async Task<ChunkBytes> CombineByteArrays(params ChunkBytes[] args)
        {
            var result = await Pool.GetAsync();

            var offSet = 0;
            for (var i = 0; i < args.Length; i++)
            {
                Buffer.BlockCopy(args[i].Bytes, 0, result.Bytes, offSet, args[i].Length);
                offSet += args[i].Length;
            }

            result.Length = offSet;
            return result;
        }


        private async Task<ChunkBytes> ReadFromStream(Stream streamSource, int chunkSize)
        {
            var result = await Pool.GetAsync();
            result.Length = await streamSource.ReadAsync(result.Bytes, 0, chunkSize);
            return result;
        }

        #endregion
    }
}