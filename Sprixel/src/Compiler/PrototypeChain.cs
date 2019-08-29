// 
// PrototypeChain.cs
//  
// Author:
//       Matthew Wilson <diakopter@gmail.com>
// 
// Copyright (c) 2010 Matthew Wilson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections;

namespace Sprixel {
    public class PrototypeChain<TKey, TValue> : Dictionary<TKey, TValue>, IEnumerable<TKey> where TValue : class {
        public bool IsRoot;
        public PrototypeChain<TKey, TValue> Parent;

        public PrototypeChain() {
            IsRoot = true;
        }

        public PrototypeChain(PrototypeChain<TKey, TValue> parent) {
            Parent = parent;
        }

        public void SetParent(PrototypeChain<TKey, TValue> parent) {
            IsRoot = null == (Parent = parent);
        }

        public void ReplaceShallowest(TKey key, TValue value) {
            this[key] = value;
        }

        new public TValue this[TKey key] {
            get {
                TValue result;
                return TryGetValue(key, out result) || IsRoot
                    ? result
                    : Parent[key];
            }
            set { // set where it exists
                var pc = this;
            donext:
                if (pc.HasOwnKey(key)) {
                    pc[key, true] = value;
                    return;
                }
                if (!pc.IsRoot) {
                    pc = pc.Parent;
                    goto donext;
                }
                throw new KeyNotFoundException(key + " not found");
            }
        }

        public TValue GetFromHere(TKey key) {
            return base[key];
        }

        public void SetHere(TKey key, TValue value) {
            base[key] = value;
        }

        public TValue this[TKey key, bool flag1] {
            get {
                return base[key];
            }
            set {
                base[key] = value;
            }
        }

        public void AddHere(TKey key, TValue value) {
            if (null != value)
                Add(key, value);
        }

        public TValue this[TKey key, bool flag1, bool flag2] {
            set {
                if (null != value)
                    Add(key, value);
            }
        }

        public TKey[] KeysArray {
            get {
                TKey[] keys;
                if (IsRoot) {
                    var keyCollection = base.Keys;
                    keys = new TKey[keyCollection.Count];
                    base.Keys.CopyTo(keys, 0);
                    return keys;
                } else {
                    var own = base.Keys;
                    var parents = Parent.Keys;
                    var ownCount = own.Count;
                    keys = new TKey[ownCount + parents.Count];
                    own.CopyTo(keys, 0);
                    parents.CopyTo(keys, ownCount);
                    return keys;
                }
            }
        }

        public List<TKey> KeysList {
            get {
                var list = new List<TKey>();
                var keys = KeysArray;
                for (int i = 0; i < keys.Length; i++)
                    list.Add(keys[i]);
                return list;
            }
        }

        public List<TKey> OwnKeysList {
            get {
                var list = new List<TKey>();
                foreach (var key in base.Keys)
                    list.Add(key);
                return list;
            }
        }

        new public Dictionary<TKey, TValue>.KeyCollection Keys {
            get {
                if (IsRoot)
                    return base.Keys;
                var own = base.Keys;
                var parents = Parent.Keys;
                var ownCount = own.Count;
                var keys = new TKey[ownCount + parents.Count];
                own.CopyTo(keys, 0);
                parents.CopyTo(keys, ownCount);
                var newDict = new Dictionary<TKey, TValue>();
                foreach (var key in keys) {
                    if (!newDict.ContainsKey(key))
                        newDict.Add(key, null);
                }
                return newDict.Keys;
            }
        }

        new public IEnumerator<TKey> GetEnumerator() {
            return ((IEnumerable<TKey>)Keys).GetEnumerator();
        }

        public bool HasOwnKey(TKey key) {
            return base.ContainsKey(key);
        }

        new public bool ContainsKey(TKey key) {
            return IsRoot
                ? base.ContainsKey(key)
                : base.ContainsKey(key) || Parent.ContainsKey(key);
        }

        new public bool TryGetValue(TKey key, out TValue member) {
            return IsRoot
                ? base.TryGetValue(key, out member)
                : base.TryGetValue(key, out member) || Parent.TryGetValue(key, out member);
        }
    }
}
