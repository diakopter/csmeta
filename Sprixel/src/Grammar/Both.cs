// 
// Both.cs
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TriAxis.RunSharp;

namespace Sprixel {
    public abstract class BothBase : BinaryPattern {
        public BothBase(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override Pattern Regen(Grammar g) {
            return Discern(L.Regen(g), R.Regen(g));
        }

        public override int MinLength {
            get { return L.MinLength + R.MinLength; }
        }

        public override int MaxLength {
            get { return L.MaxLength + R.MaxLength; }
        }

        public static Pattern Discern(Pattern l, Pattern r) {
            return l.Backtracking || r.Backtracking
              ? !r.Backtracking
                ? new BothLB(l, r)
                : !l.Backtracking
                  ? new BothRB(l, r) as BothBase
                  : new BothLRB(l, r)
              : new Both(l, r);
        }
    }

    public abstract class BothBacktracking : BothBase, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }
        public BothBacktracking(Pattern l, Pattern r)
            : base(l, r) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
        }

        public override bool Backtracking { get { return true; } }
    }

    public class Both : BothBase {
        public Both(Pattern l, Pattern r)
            : base(l, r) {
            Deterministic = true;
        }

        public override void Emit(Grammar g) {
            L.Fail = R.Fail = Fail;
            L.Emit(g);
            R.Emit(g);
        }

        public override bool Backtracking { get { return false; } }
    }

    public class BothLB : BothBacktracking {
        public BothLB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var rightinit = Transition.Next;
            var l = L as IBackTracking;
            var s = c["s"];
            var flag = s.Field("F");

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            L.Emit(g);

            c.Label(l.Done);
            g.Ascend(c);
            c.AssignOr(flag, StateFlag.LeftDone);

            c.Label(rightinit);
            R.Emit(g);
            c.GotoTrue(flag & StateFlag.LeftDone, Done);
            c.Goto(Notd);

            c.Label(l.Notd);
            c.Assign(s, s.Property("TLI"));
            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.LeftDone);
            c.Goto(rightinit);

            c.Label(L.Fail);
            g.Ascend(c);
            c.Goto(Fail);

            c.Label(R.Fail);
            c.If((flag & StateFlag.LeftDone) != uint.MinValue);
            {
                g.Ascend(c);
                c.Goto(Fail);
            }
            c.End();
            c.Assign(c["o"], s.Field("S"));

            c.Label(Back);
            c.Assign(s, s.Field("L"));
            c.Goto(l.Back);
        }
    }

    public class BothRB : BothBacktracking {
        public BothRB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            L.Fail = Fail;
            var c = g.CodeGen;
            var r = R as IBackTracking;
            var s = c["s"];
            var flag = s.Field("F");

            c.Label(Init);
            L.Emit(g);
            c.Assign(s, s.Invoke("D", c["o"]));
            R.Emit(g);

            c.Label(r.Done);
            g.Ascend(c);
            c.Goto(Done);

            c.Label(r.Notd);
            //c.WriteLine("bothrb notd s.id " + s.Field("id") + " " + L.ToString() + " " + R.ToString());
            c.Assign(s, s.Property("TRI"));
            c.Goto(Notd);

            c.Label(Back);
            c.Assign(s, s.Field("R"));
            c.Assign(c["o"], s.Field("S"));
            c.Goto(r.Back);

            c.Label(R.Fail);
            g.Ascend(c);
            c.Goto(Fail);
        }
    }

    public class BothLRB : BothBacktracking {
        public BothLRB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var rightinit = Transition.Next;
            var l = L as IBackTracking;
            var r = R as IBackTracking;
            var s = c["s"];
            var flag = s.Field("F");

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            L.Emit(g);

            c.Label(l.Done);
            g.Ascend(c);
            c.AssignOr(flag, StateFlag.LeftDone);
            c.Goto(rightinit);

            c.Label(l.Notd);
            c.Assign(s, s.Property("TLI"));
            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.LeftDone);

            c.Label(rightinit);
            R.Emit(g);

            c.Label(r.Done);
            g.Ascend(c);
            c.GotoTrue(flag & StateFlag.LeftDone, Done);
            c.Goto(Notd);

            c.Label(r.Notd);
            //c.WriteLine("bothlrb notd s.id " + s.Field("id"));
            c.Assign(s, s.Property("TRI"));
            c.Goto(Notd);

            c.Label(L.Fail);
            g.Ascend(c);
            c.Goto(Fail);

            c.Label(Back);
            c.If((flag & StateFlag.LeftDone) == uint.MinValue);
            {
                c.Assign(c["o"], s.Field("S"));
                c.Assign(s, s.Field("L"));
                c.Goto(l.Back);
            }
            c.End();
            c.Assign(s, s.Field("R"));
            c.Assign(c["o"], s.Field("S"));
            c.Goto(r.Back);

            c.Label(R.Fail);
            c.If((flag & StateFlag.LeftDone) == uint.MinValue);
            {
                c.Assign(c["o"], s.Field("S"));
                c.Assign(s, s.Field("L"));
                c.Goto(l.Back);
            }
            c.End();
            g.Ascend(c);
            c.Goto(Fail);
        }
    }
}
