using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace S3Sync.Core
{
    public static class S3ClientExtensions
    {
        /// <summary>
        /// Convert to Enumerable S3Object
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<S3Object> ToS3Objects(this IEnumerable<ListObjectsV2Response> source)
        {
            return source.SelectMany(x => x.S3Objects);
        }

        /// <summary>
        /// Pick up raw ETag string without "" defined in RFC
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetEtag(this GetObjectResponse source)
        {
            if (source == null) throw new ArgumentNullException();
            return source.ETag.Replace("\"", "");
        }

        /// <summary>
        /// Pick up raw ETag string without "" defined in RFC
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetETag(this S3Object source)
        {
            if (source == null) throw new ArgumentNullException();
            return source.ETag.Replace("\"", "");
        }

        /// <summary>
        /// Pick up Chunk count when Multipart ETag.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetETagChunkCount(this S3Object source)
        {
            if (source == null) throw new ArgumentNullException();

            var eTag = source.GetETag();
            if (eTag.Length == 32) return 0;
            return int.Parse(eTag.Split('-')[1]);
        }

        /// <summary>
        /// Return S3Object except KeyPrefix is startswith specified.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="keyPrefix"></param>
        /// <returns></returns>
        public static IEnumerable<S3Object> IgnorePrefix(this IEnumerable<S3Object> source, string keyPrefix)
        {
            return source.Where(x => !x.Key.StartsWith(keyPrefix));
        }
    }
}
