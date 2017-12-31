using System;
using System.Collections.Generic;
using System.Text;

namespace S3Sync.Core
{
    public static class StringExtensions
    {
        /// <summary>
        /// Concat string arrays into single string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToJoinedString<T>(this IEnumerable<T> source, string separator = "")
        {
            return String.Join(separator, source);
        }

        public static string[] SplitEx(this string input, string separator)
        {
#if NETCOREAPP2_0
            return input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
#else
            return input.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
#endif
        }
    }
}
