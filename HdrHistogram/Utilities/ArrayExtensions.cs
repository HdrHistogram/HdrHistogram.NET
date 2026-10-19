using System;

namespace HdrHistogram.Utilities
{
    /// <summary>
    /// Extension methods for Arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks if the two arrays have the same items in the same order.
        /// </summary>
        /// <typeparam name="T">The type of the items in the arrays.</typeparam>
        /// <param name="source">The source array to check.</param>
        /// <param name="other">The other array to check against.</param>
        /// <returns>Returns <c>true</c> if the arrays are of the same length and each item by index is equal, else <c>false</c>.</returns>
        public static bool IsSequenceEqual<T>(this T[] source, T[] other)
        {
            if (source.Length != other.Length)
                return false;

            // Primitive equality agrees with object.Equals; preserve custom equality
            // and covariant reference arrays by keeping their existing comparison path.
            if (typeof(T).IsPrimitive)
                return source.AsSpan().SequenceEqual(other.AsSpan(), comparer: null);

            for (int i = 0; i < other.Length; i++)
            {
                if (!Equals(source[i], other[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
