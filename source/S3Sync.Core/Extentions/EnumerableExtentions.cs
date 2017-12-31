using System;
using System.Collections.Generic;

namespace S3Sync.Core.Extentions
{
    public static class EnumerableExtentions
    {
        public static IEnumerable<T[]> Buffer<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0) throw new ArgumentOutOfRangeException("count");

            return BufferCore(source, count);
        }

        static IEnumerable<T[]> BufferCore<T>(this IEnumerable<T> source, int count)
        {
            var buffer = new T[count];
            var index = 0;
            foreach (var item in source)
            {
                buffer[index++] = item;
                if (index == count)
                {
                    yield return buffer;
                    index = 0;
                    buffer = new T[count];
                }
            }

            if (index != 0)
            {
                var dest = new T[index];
                Array.Copy(buffer, dest, index);
                yield return dest;
            }
        }
    }
}
