using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel.Runtime {
    public class buffer<T> {
        static readonly int bits;
        static readonly int bytes;
        static readonly Type type;
        static buffer() {
            System.Type t = type = typeof(T);
            int numbytes = t.Equals(typeof(ulong)) || t.Equals(typeof(long))
                ? 8
                : t.Equals(typeof(uint)) || t.Equals(typeof(int))
                    ? 4
                    : t.Equals(typeof(ushort)) || t.Equals(typeof(short))
                        ? 2
                        : t.Equals(typeof(byte))
                            ? 1
                            : 0;
            bits = 8 * numbytes;
            bytes = numbytes;
        }

        public int Bits { get { return buffer<T>.bits; } }
        public int Bytes { get { return buffer<T>.bytes; } }
        public Type Contained { get { return buffer<T>.type; } }
        public int Length;

        public buffer(int length) { Array = new T[Length = length]; }

        public T[] Array;
        public T this[int index] { get { return Array[index]; } set { Array[index] = value; } }

        public void BlockCopy(int srcOffset, buffer<T> dst, int dstOffset, int count) {
            Buffer.BlockCopy(Array, srcOffset * Bytes, dst.Array, dstOffset * dst.Bytes, count);
        }

        public void BlockCopy(int srcOffset, int dstOffset, int count) {
            Buffer.BlockCopy(Array, srcOffset * Bytes, Array, dstOffset * Bytes, count);
        }
    }

    public class buf8 : List<Byte> {
        public buf8()
            : base() {
        }
        new public Byte this[int index] {
            get {
                return base[index];
            }
            set {
                if (index >= base.Count) {
                    var more = index - base.Count + 1;
                    do {
                        base.Add(0);
                    } while (--more > 0);
                }
                base[index] = value;
            }
        }
        public int Size { // in bytes
            get {
                return Capacity;
            }
        }
    }
    public class buf16 : List<UInt16> {
        public buf16()
            : base() {
        }
        new public UInt16 this[int index] {
            get {
                return base[index];
            }
            set {
                if (index >= base.Count) {
                    var more = index - base.Count + 1;
                    do {
                        base.Add(0);
                    } while (--more > 0);
                }
                base[index] = value;
            }
        }
        public int Size { // in bytes
            get {
                return Capacity * 2;
            }
        }
    }
    public class buf32 : List<UInt32> {
        public buf32()
            : base() {
        }
        new public UInt32 this[int index] {
            get {
                return base[index];
            }
            set {
                if (index >= base.Count) {
                    var more = index - base.Count + 1;
                    do {
                        base.Add(0);
                    } while (--more > 0);
                }
                base[index] = value;
            }
        }
        public int Size { // in bytes
            get {
                return Capacity * 4;
            }
        }
    }
    public class buf64 : List<UInt64> {
        public buf64()
            : base() {
        }
        new public UInt64 this[int index] {
            get {
                return base[index];
            }
            set {
                if (index >= base.Count) {
                    var more = index - base.Count + 1;
                    do {
                        base.Add(0);
                    } while (--more > 0);
                }
                base[index] = value;
            }
        }
        public int Size { // in bytes
            get {
                return Capacity * 8;
            }
        }
    }
}