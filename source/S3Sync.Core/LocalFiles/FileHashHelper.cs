using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace S3Sync.Core.LocalFiles
{
    public static class FileHashHelper
    {
        private static readonly HashAlgorithm md5 = MD5.Create();

        /// <summary>
        /// Get Chunksize
        /// </summary>
        /// <param name="baseSize"></param>
        /// <param name="chunkCount"></param>
        /// <returns></returns>
        public static int GetChunkSize(long baseSize, int chunkCount)
        {
            if (chunkCount == 0) return 0;

            // use bit calculation to detect power of the 2
            // 10000 & 11111 should be 0.
            var modArrange = (baseSize & baseSize - 1) == 0 ? 0 : 1;

            // Calculate chunksize (if mod exists than - chunkCount)
            return EstimateChunkSize((decimal)baseSize / (chunkCount - modArrange));
        }

        /// <summary>
        /// Estimate Max available 2^n Chunk size.
        /// </summary>
        /// <remarks>http://docs.aws.amazon.com/ja_jp/AmazonS3/latest/dev/qfacts.html</remarks>
        /// <param name="baseSize"></param>
        /// <returns></returns>
        public static int EstimateChunkSize(decimal baseSize)
        {
            if (baseSize < 536870912) // 512MB
            {
                if (baseSize < 33554432) // 32MB
                {
                    if (baseSize < 16777216) // 16MB
                    {
                        if (baseSize < 8388608) // 8MB
                        {
                            return 5;
                        }
                        else
                        {
                            return 8;
                        }
                    }
                    else
                    {
                        return 16;
                    }
                }
                else
                {
                    if (baseSize < 134217728) // 128MB
                    {
                        if (baseSize < 67108864) // 64MB
                        {
                            return 32;
                        }
                        else
                        {
                            return 64;
                        }
                    }
                    else
                    {
                        if (baseSize < 268435456) // 256MB
                        {
                            return 128;
                        }
                        else
                        {
                            return 256;
                        }
                    }
                }
            }
            else
            {
                if (baseSize < 2147483648) // 2,048MB
                {
                    if (baseSize < 1073741824) // 1,024MB
                    {
                        return 512;
                    }
                    else
                    {
                        return 1024;
                    }
                }
                else
                {
                    if (baseSize < 4294967296) // 4,096MB
                    {
                        return 2048;
                    }
                    else
                    {
                        return 4096;
                    }
                }
            }
        }

        public static byte[] GetFileBinary(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, fileBytes.Length);
                return fileBytes;
            }
        }

        public static byte[] GetMD5Hash(this byte[] array)
        {
            var hash = GetHash(array, md5);
            return hash;
        }

        public static byte[] GetHash(this byte[] array, HashAlgorithm algorithm)
        {
            var hash = algorithm.ComputeHash(array);
            return hash;
        }

        public static string CalculateEtag(byte[] array, int chunkCount)
        {
            using (var md5 = MD5.Create())
            {
                if (chunkCount == 0)
                {
                    return array.GetHash(md5).ToHexString();
                }

                var multipartSplitCount = 0;
                var chunkSize = 1024 * 1024 * chunkCount;
                var splitCount = array.Length / chunkSize;
                var mod = array.Length - chunkSize * splitCount;
                IEnumerable<byte> concatHash = Enumerable.Empty<byte>();

                for (var i = 0; i < splitCount; i++)
                {
                    var offset = i == 0 ? 0 : chunkSize * i;
                    var chunk = GetSegment(array, offset, chunkSize);
                    var hash = chunk.ToArray().GetHash(md5);
                    concatHash = concatHash.Concat(hash);
                    multipartSplitCount++;
                }
                if (mod != 0)
                {
                    var chunk = GetSegment(array, chunkSize * splitCount, mod);
                    var hash = chunk.ToArray().GetHash(md5);
                    concatHash = concatHash.Concat(hash);
                    multipartSplitCount++;
                }
                var multipartHash = concatHash.ToArray().GetHash(md5).ToHexString();

                if (multipartSplitCount <= 0)
                {
                    return multipartHash;
                }
                else
                {
                    return multipartHash + "-" + multipartSplitCount;
                }
            }
        }

        private static ArraySegment<T> GetSegment<T>(this T[] array, int offset, int? count = null)
        {
            if (count == null) { count = array.Length - offset; }
            return new ArraySegment<T>(array, offset, count.Value);
        }

        public static string ToHashString(this byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static string ToHexString(this byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                chars[2 * i] = ToHexDigit(bytes[i] / 16);
                chars[2 * i + 1] = ToHexDigit(bytes[i] % 16);
            }

            return new string(chars).ToLower();
        }

        private static char ToHexDigit(int i)
        {
            if (i < 10)
            {
                return (char)(i + '0');
            }
            return (char)(i - 10 + 'A');
        }
    }
}
