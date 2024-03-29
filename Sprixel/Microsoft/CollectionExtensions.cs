/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Sprixel {
    internal static class CollectionExtensions {
        /// <summary>
        /// Wraps the provided enumerable into a ReadOnlyCollection{T}
        /// 
        /// Copies all of the data into a new array, so the data can't be
        /// changed after creation. The exception is if the enumerable is
        /// already a ReadOnlyCollection{T}, in which case we just return it.
        /// </summary>
        internal static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) {
                return EmptyReadOnlyCollection<T>.Instance;
            }

            var roCollection = enumerable as ReadOnlyCollection<T>;
            if (roCollection != null) {
                return roCollection;
            }

            var collection = enumerable as ICollection<T>;
            if (collection != null) {
                int count = collection.Count;
                if (count == 0) {
                    return EmptyReadOnlyCollection<T>.Instance;
                }

                T[] array = new T[count];
                collection.CopyTo(array, 0);
                return new ReadOnlyCollection<T>(array);
            }

            // ToArray trims the excess space and speeds up access
            return new ReadOnlyCollection<T>(new List<T>(enumerable).ToArray());
        }

        // We could probably improve the hashing here
        internal static int ListHashCode<T>(this IEnumerable<T> list) {
            var cmp = EqualityComparer<T>.Default;
            int h = 6551;
            foreach (T t in list) {
                h ^= (h << 5) ^ cmp.GetHashCode(t);
            }
            return h;
        }

        internal static bool ListEquals<T>(this ICollection<T> first, ICollection<T> second) {
            if (first.Count != second.Count) {
                return false;
            }
            var cmp = EqualityComparer<T>.Default;
            var f = first.GetEnumerator();
            var s = second.GetEnumerator();
            while (f.MoveNext()) {
                s.MoveNext();

                if (!cmp.Equals(f.Current, s.Current)) {
                    return false;
                }
            }
            return true;
        }

        internal static IEnumerable<U> Select<T, U>(this IEnumerable<T> enumerable, Func<T, U> select) {
            foreach (T t in enumerable) {
                yield return select(t);
            }
        }

        // Name needs to be different so it doesn't conflict with Enumerable.Select
        internal static U[] Map<T, U>(this ICollection<T> collection, Func<T, U> select) {
            int count = collection.Count;
            U[] result = new U[count];
            count = 0;
            foreach (T t in collection) {
                result[count++] = select(t);
            }
            return result;
        }

        internal static IEnumerable<T> Where<T>(this IEnumerable<T> enumerable, Func<T, bool> where) {
            foreach (T t in enumerable) {
                if (where(t)) {
                    yield return t;
                }
            }
        }

        internal static List<T> ToList<T>(this IEnumerable<T> enumerable) {
            return new List<T>(enumerable);
        }

        internal static T[] ToArray<T>(this IEnumerable<T> enumerable) {
            var c = enumerable as ICollection<T>;
            if (c != null) {
                var result = new T[c.Count];
                c.CopyTo(result, 0);
                return result;
            }
            return new List<T>(enumerable).ToArray();
        }

        internal static bool Any<T>(this IEnumerable<T> source) {
            using (IEnumerator<T> e = source.GetEnumerator()) {
                return e.MoveNext();
            }
        }

        internal static bool Any<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
            foreach (T element in source) {
                if (predicate(element)) {
                    return true;
                }
            }
            return false;
        }

        internal static bool All<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
            foreach (T element in source) {
                if (!predicate(element)) {
                    return false;
                }
            }
            return true;
        }

        internal static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func) {
            using (IEnumerator<TSource> e = source.GetEnumerator()) {
                if (!e.MoveNext()) throw new ArgumentException("Collection is empty", "source");
                TSource result = e.Current;
                while (e.MoveNext()) result = func(result, e.Current);
                return result;
            }
        }

        internal static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) {
            TAccumulate result = seed;
            foreach (TSource element in source) result = func(result, element);
            return result;
        }

        internal static T[] RemoveFirst<T>(this T[] array) {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 1, result, 0, result.Length);
            return result;
        }

        internal static T[] RemoveLast<T>(this T[] array) {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 0, result, 0, result.Length);
            return result;
        }

        internal static T[] AddFirst<T>(this IList<T> list, T item) {
            T[] res = new T[list.Count + 1];
            res[0] = item;
            list.CopyTo(res, 1);
            return res;
        }

        internal static T[] AddLast<T>(this IList<T> list, T item) {
            T[] res = new T[list.Count + 1];
            list.CopyTo(res, 0);
            res[list.Count] = item;
            return res;
        }

        internal static T[] RemoveAt<T>(this T[] array, int indexToRemove) {
            Debug.Assert(array != null);
            Debug.Assert(indexToRemove >= 0 && indexToRemove < array.Length);

            T[] result = new T[array.Length - 1];
            if (indexToRemove > 0) {
                Array.Copy(array, 0, result, 0, indexToRemove);
            }
            int remaining = array.Length - indexToRemove - 1;
            if (remaining > 0) {
                Array.Copy(array, array.Length - remaining, result, result.Length - remaining, remaining);
            }
            return result;
        }

        internal static T[] RotateRight<T>(this T[] array, int count) {
            Debug.Assert(count >= 0 && count <= array.Length);

            T[] result = new T[array.Length];
            // The head of the array is shifted, and the tail will be rotated to the head of the resulting array
            int sizeOfShiftedArray = array.Length - count;
            Array.Copy(array, 0, result, count, sizeOfShiftedArray);
            Array.Copy(array, sizeOfShiftedArray, result, 0, count);
            return result;
        }
    }


    internal static class EmptyReadOnlyCollection<T> {
        internal static ReadOnlyCollection<T> Instance = new ReadOnlyCollection<T>(new T[0]);
    }
    // TODO: Should we use this everywhere for empty arrays?
    // my thought is, probably more hassle than its worth
    internal static class EmptyArray<T> {
        internal static T[] Instance = new T[0];
    }
}
