using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel {
    public sealed class MState {
        public Stack<State> S; // backtrackstack
        public Stack<int> E; // endpoints
        public HashSet<uint> T = new HashSet<uint>(); // tried offset+length pairs
        public HashSet<int> R = new HashSet<int>(); // returned offsets
        public State O; // owner
        public int B; // number backtrackable

        public MState(State owner) {
            O = owner;
            owner.M = this;
            S = new Stack<State>(32);
            E = new Stack<int>(32);
        }

        public MState(State owner, int max) {
            O = owner;
            owner.M = this;
            S = new Stack<State>(max < 1000 ? max : 1000);
            E = new Stack<int>(max < 1000 ? max : 1000);
        }

        public void TriedAt(int offset) {
            T.Add(unchecked(((uint)S.Count << 16) + (uint)offset));
        }

        public bool Tried(int offset) {
            return T.Contains(unchecked(((uint)S.Count << 16) + (uint)offset));
        }

        public State PushAscend(State state, int offset) {
            S.Push(state);
            E.Push(offset);
            return O;
        }

        public State PushAscend(State state, int offset, bool notDone) {
            S.Push(state);
            state.F |= StateFlag.SelfNotDone;
            E.Push(offset);
            ++B;
            return O;
        }

        public void ReturnedAt(int offset) {
            R.Add(offset);
        }

        public bool Returned(int offset) {
            return R.Contains(offset);
        }

        public int Count {
            get {
                return S.Count;
            }
        }
    }
}
