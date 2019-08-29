using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel.Runtime {
    public class array<T> : List<T> {

        public array() : base() {

        }

        new public T this[int index] {
            get {
                return this[index];
            }
            set {
                int remaining = 0;
                if (Count < index) {
                    if (Capacity < index)
                        Capacity += index;
                    remaining = index - Count;
                    while (--remaining >= 0) {
                        Add(default(T));
                    }
                }
                base[index] = value;
            }
        }
    }
}
