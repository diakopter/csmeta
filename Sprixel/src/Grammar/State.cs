using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel {
    public static class StateFlag {
        public const uint Clear = 0;
        public const uint LeftDone = 1;
        public const uint RightDone = LeftDone << 1;
        public const uint RightInited = RightDone << 1;
        public const uint SelfNotDone = RightInited << 1;
    }

    public sealed class State {
        public static State FirstState = new State(null, 0);

        public static State GetFirstState() {
            return FirstState;
        }

        public State L;
        public State R;
        public State I;
        public int S;
        public uint F;
        public MState M;
        public uint id;

        public State(State invoker, int offset) {
            I = invoker;
            S = offset;
            id = Transition.Next.GetUint();
        }

        public State TLI {
            get {
                I.L = this;
                return I;
            }
        }
        
        public State TRI {
            get {
                I.R = this;
                return I;
            }
        }

        public State D(int offset) {
            return new State(this, offset);
        }

        public State DM(int offset)
        {
            var s = new State(this, offset);
            s.M = new MState(s);
            return s;
        }

        public State DM(int offset, int max)
        {
            var s = new State(this, offset);
            s.M = new MState(s, max);
            return s;
        }
    }
}
