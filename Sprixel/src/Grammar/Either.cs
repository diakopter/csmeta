// 
// Either.cs
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
    public abstract class EitherBase : BinaryPattern {
        public EitherBase(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override Pattern Regen(Grammar g) {
            return Discern(L.Regen(g), R.Regen(g));
        }

        public override int MinLength {
            get { return Math.Min(L.MinLength, R.MinLength); }
        }

        public override int MaxLength {
            get { return Math.Max(L.MaxLength, R.MaxLength); }
        }

        public static Pattern Discern(Pattern l, Pattern r) {
            return l.Backtracking || r.Backtracking
              ? !r.Backtracking
                ? new EitherLB(l, r)
                : !l.Backtracking
                  ? new EitherRB(l, r) as EitherBase
                  : new EitherLRB(l, r)
              : new Either(l, r);
        }
    }

    public abstract class EitherBacktracking : EitherBase, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }
        public EitherBacktracking(Pattern l, Pattern r)
            : base(l, r) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
        }
        public override bool Backtracking { get { return true; } }
    }

    public class Either : EitherBacktracking {
        public Either(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var l = L as IBackTracking;
            var s = c["s"];
            Transition mydone = Transition.Next;

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            L.Emit(g);

            c.Goto(Backtracking
                ? Notd
                : mydone);

            c.Label(R.Fail);
            g.Ascend(c);
            c.Goto(Fail);

            c.Label(L.Fail);
            c.Assign(c["o"], s.Field("S"));

            c.Label(Back);
            R.Emit(g);

            if (Backtracking)
                c.Goto(Done);
            else
                c.Label(mydone);
        }

        //public override bool Backtracking {
        //    get {
        //        return L.MinLength != R.MinLength;
        //    }
        //}
    }

    public class EitherLB : EitherBacktracking {
        public EitherLB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var l = L as IBackTracking;
            var s = c["s"];
            var flag = s.Field("F");

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            //c.WriteLine("eitherlb init s.id " + s.Field("id"));
            L.Emit(g);

            c.Label(l.Done);
            g.Ascend(c);
            //c.WriteLine("eitherlb l.done s.id " + s.Field("id"));
            c.AssignOr(flag, StateFlag.LeftDone);
            c.Goto(Notd);

            c.Label(l.Notd);
            c.Assign(s, s.Property("TLI"));
            //c.WriteLine("eitherlb l.notd s.id " + s.Field("id"));
            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.LeftDone);
            c.Goto(Notd);

            c.Label(Back);
            //c.WriteLine("eitherlb back s.id " + s.Field("id"));
            c.GotoTrue(flag & StateFlag.LeftDone, L.Fail);
            c.Assign(s, s.Field("L"));
            c.Goto(l.Back);

            c.Label(R.Fail);
            //c.WriteLine("eitherlb r.fail s.id " + s.Field("id"));
            g.Ascend(c);
            c.Goto(Fail);

            c.Label(L.Fail);
            //c.WriteLine("eitherlb l.fail s.id " + s.Field("id"));
            c.Assign(c["o"], s.Field("S"));
            R.Emit(g);

            c.Goto(Done);
        }
    }

    public class EitherRB : EitherBacktracking {
        public EitherRB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var r = R as IBackTracking;
            var s = c["s"];
            var o = c["o"];
            var flag = s.Field("F");

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            L.Emit(g);

            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.RightInited);
            c.Goto(Notd);

            c.Label(L.Fail);
            ////c.WriteLine("eitherrb l.fail at " + o + " of " + c["l"]);
            ////c.WriteLine(s.Field("id"));
            c.AssignOr(flag, StateFlag.RightInited);
            c.Assign(c["o"], s.Field("S"));
            R.Emit(g);

            c.Label(r.Done);
            g.Ascend(c);
            c.Goto(Done);

            c.Label(r.Notd);
            //c.WriteLine("eitherrb notd s.id " + s.Field("id"));
            c.Assign(s, s.Property("TRI"));
            c.Goto(Notd);

            c.Label(Back);
            //c.WriteLine("eitherrb back s.id " + s.Field("id"));
            c.If((flag & StateFlag.RightInited) != 0);
            {
                c.Assign(s, s.Field("R"));
                c.Goto(r.Back);
            }
            c.End();
            c.Goto(L.Fail);

            c.Label(R.Fail);
            g.Ascend(c);
            c.Goto(Fail);
        }
    }

    public class EitherLRB : EitherBacktracking {
        public EitherLRB(Pattern l, Pattern r)
            : base(l, r) {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var l = L as IBackTracking;
            var r = R as IBackTracking;
            var s = c["s"];
            var o = c["o"];
            var flag = s.Field("F");

            c.Label(Init);
            c.Assign(s, s.Invoke("D", c["o"]));
            //c.WriteLine("eitherlrb init s.id " + s.Field("id"));
            L.Emit(g);

            c.Label(l.Done);
            g.Ascend(c);
            //c.WriteLine("eitherlrb l.done s.id " + s.Field("id"));
            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.RightInited);
            c.AssignOr(flag, StateFlag.LeftDone);
            c.Goto(Notd);

            c.Label(l.Notd);
            c.Assign(s, s.Property("TLI"));
            //c.WriteLine("eitherlrb l.notd s.id " + s.Field("id"));
            c.AssignAnd(flag, uint.MaxValue ^ StateFlag.LeftDone);
            c.Goto(Notd);

            c.Label(L.Fail);
            //c.WriteLine("eitherlrb l.fail s.id " + s.Field("id"));
            c.AssignOr(flag, StateFlag.RightInited | StateFlag.LeftDone);
            c.Assign(c["o"], s.Field("S"));
            R.Emit(g);

            c.Label(r.Done);
            g.Ascend(c);
            //c.WriteLine("eitherlrb r.done s.id " + s.Field("id"));
            c.Goto(Done);

            c.Label(r.Notd);
            c.Assign(s, s.Property("TRI"));
            //c.WriteLine("eitherlrb r.notd s.id " + s.Field("id"));
            c.Goto(Notd);

            c.Label(Back);
            //c.WriteLine("eitherlrb back s.id " + s.Field("id"));
            c.If((flag & StateFlag.LeftDone) == uint.MinValue);
            {
                c.Assign(s, s.Field("L"));
                c.Goto(l.Back);
            }
            c.Else();
            {
                c.If((flag & StateFlag.RightInited) == uint.MinValue);
                {
                    c.Goto(L.Fail);
                }
                c.Else();
                {
                    c.Assign(s, s.Field("R"));
                    c.Goto(r.Back);
                }
                c.End();
            }
            c.End();

            c.Label(R.Fail);
            //c.WriteLine("eitherlrb r.fail s.id " + s.Field("id"));
            g.Ascend(c);
            c.Goto(Fail);
        }
    }
}
