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
#if CLR2 || SILVERLIGHT
using System.Collections;
using System.Collections.Generic;

namespace Sprixel {

    /// <summary>
    /// A simple hashset, built on Dictionary{K, V}
    /// </summary>
    public sealed class HashSet<T> : ICollection<T> {
        private readonly Dictionary<T, object> _data;

        public HashSet() {
            _data = new Dictionary<T, object>();
        }

        public HashSet(IEqualityComparer<T> comparer) {
            _data = new Dictionary<T, object>(comparer);
        }

        public HashSet(IList<T> list) {
            _data = new Dictionary<T, object>(list.Count);
            foreach (T t in list) {
                _data.Add(t, null);
            }
        }

        public void Add(T item) {
            _data[item] = null;
        }

        public void Clear() {
            _data.Clear();
        }

        public bool Contains(T item) {
            return _data.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _data.Keys.CopyTo(array, arrayIndex);
        }

        public int Count {
            get { return _data.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(T item) {
            return _data.Remove(item);
        }

        public IEnumerator<T> GetEnumerator() {
            return _data.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _data.Keys.GetEnumerator();
        }

        public void UnionWith(IEnumerable<T> other) {
            foreach (T t in other) {
                Add(t);
            }
        }
    }
}
#endif
