using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings
{
    public static class Linq
    {
        public static bool IsUniform<T, TValue>(this IEnumerable<T> enumerable, Func<T, TValue> selector)
        {
            var e = enumerable.GetEnumerator();
            TValue value;
            if (e.MoveNext())
            {
                value = selector(e.Current);
            }
            else
            {
                return true;
            }

            while (e.MoveNext())
            {
                TValue value2 = selector(e.Current);
                //if (object.Equals(value, value2))
                if (!EqualityComparer<TValue>.Default.Equals(value, value2))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable, int count)
        {
            if (enumerable is ICollection<T> collection)
            {
                T[] copy = new T[collection.Count];
                collection.CopyTo(copy, 0);
                for (int i = 0, imax = collection.Count - count; i < imax; i++)
                {
                    yield return copy[i];
                }
            }
            else
            {
                Queue<T> queue = new Queue<T>(count + 1);

                var e = enumerable.GetEnumerator();
                while (e.MoveNext() && queue.Count < count)
                {
                    queue.Enqueue(e.Current);
                }

                while (e.MoveNext())
                {
                    queue.Enqueue(e.Current);
                    yield return queue.Dequeue();
                }
            }
        }
    }
}
