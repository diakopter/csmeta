// 
// Repetition.cs
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
using TriAxis.RunSharp.Operands;

namespace Sprixel {
    public class Repetition : UnaryPattern, IBackTracking {
        public int Min;
        public int Max;
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public Repetition(Pattern l, int min, int max)
            : base(l) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
            Min = min;
            Max = max;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var v = c["v"];
            var o = c["o"];
            Transition retry = Transition.Next;
            if (L.Backtracking)
            {
                var l = L as IBackTracking;
                var m = s.Field("M");
                var im = s.Field("I").Field("M");
                var flag = s.Field("F");
                var count = m.Property("Count");
                var returned = m.Invoke("Returned", o);
                var tried = m.Invoke("Tried", o);
                var backtrackable = m.Field("B");

                c.Label(Init);
                //c.WriteLine("rep init at " + o);
                if (Max == -1)
                    c.Assign(s, s.Invoke("DM", o));
                else
                    c.Assign(s, s.Invoke("DM", o, Max));
                //c.WriteLine(s.Field("id"));
                c.Label(retry);
                //c.WriteLine(Init + "brep trying one at " + o);
                c.Invoke(m, "TriedAt", o);

                L.Emit(g);

                c.Label(l.Done);
                //c.WriteLine("rep l.done at " + o);
                //c.WriteLine(s.Field("I").Field("id"));
                c.If(o == s.Field("S")); // zero-length match (but complete)
                {
                    c.AssignAnd(flag, uint.MaxValue ^ StateFlag.SelfNotDone);
                    c.Assign(s, im.Invoke("PushAscend", s, o));
                    c.Goto(L.Fail); // pretend that we went to the next child and it succeeded.
                }
                c.End();
                c.AssignAnd(flag, uint.MaxValue ^ StateFlag.SelfNotDone);
                c.Assign(s, im.Invoke("PushAscend", s, o));
                c.GotoTrue(tried, Back);
                //c.WriteLine("rep have not tried at " + o);
                c.GotoFalse(Max == -1
                    ? tried
                    : (count >= Max || tried), retry);
                c.If(count > Min || (count == Min && backtrackable > 0));
                {
                    c.Invoke(m, "ReturnedAt", o);
                    //c.WriteLine("rep returning notd at " + o);
                    c.Goto(Notd);
                }
                c.End();
                c.GotoTrue(count < Min, Back);
                //c.WriteLine("rep returning done at " + o);
                c.Goto(Done);

                c.Label(l.Notd);
                //c.WriteLine("rep l.notd at " + o);
                //c.WriteLine(s.Field("I").Field("id"));
                c.Assign(s, im.Invoke("PushAscend", s, o, true));
                c.GotoTrue(tried, Back);
                //c.WriteLine("rep have not tried at " + o);
                c.GotoFalse(Max == -1
                    ? tried
                    : (count >= Max || tried), retry);
                c.GotoTrue(count < Min, Back);
                c.Invoke(m, "ReturnedAt", o);
                //c.WriteLine("rep returning notd at " + o);
                c.Goto(Notd);

                c.Label(Back);
                //c.WriteLine("rep back at " + o);
                //c.WriteLine(s.Field("id"));
                //c.WriteLine("backtrackable: " + backtrackable);
                var s2 = c["s2"];
                var c1 = c["i2"];
                c.If(count == 0);
                {
                    g.Ascend(c);
                    c.Goto(Fail);
                }
                c.End();
                c.Assign(s2, m.Field("S").Invoke("Pop"));
                c.Invoke(m.Field("E"), "Pop");
                c.If((s2.Field("F") & StateFlag.SelfNotDone) > 0);
                {
                    c.Decrement(backtrackable);
                    c.Assign(o, s2.Field("S"));
                    c.Assign(s, s2);
                    //c.WriteLine("rep l.back at " + o);
                    c.Goto(l.Back);
                }
                c.End();
                c.Assign(o, s2.Field("S"));
                c.If(returned.LogicalNot());
                {
                    c.If(count > Min || (count == Min && backtrackable > 0));
                    {
                        c.Invoke(m, "ReturnedAt", o);
                        //c.WriteLine("rep returning notd at " + o);
                        c.Goto(Notd);
                    }
                    c.End();
                    c.GotoTrue(backtrackable > 0, Back);
                    //c.WriteLine("rep returning done at " + o);
                    c.Goto(Done);
                }
                c.End();

                c.Label(L.Fail);
                //c.WriteLine("rep l.fail at " + o);
                //c.WriteLine(s.Field("id"));
                c.If(count < Min);
                {
                    c.If(count > 0);
                    {
                        c.Assign(o, m.Field("E").Invoke("Peek"));
                    }
                    c.Else();
                    {
                        c.Assign(o, s.Field("S"));
                    }
                    c.End();
                    //c.If(returned.LogicalNot());
                    //c.WriteLine("rep retrying; has not already returned at " + o);
                    //c.End();
                    c.GotoFalse(tried, retry);
                    c.If(backtrackable > 0);
                    //c.WriteLine("backtrackable > 0 " + backtrackable);
                    c.End();
                    c.GotoTrue(backtrackable > 0, Back);
                    //c.WriteLine("rep fail at " + o);
                    //c.WriteLine(s.Field("id"));
                    c.Assign(o, s.Field("S"));
                    g.Ascend(c);
                    c.Goto(Fail);
                }
                c.End();
                c.If(backtrackable > 0 || count > Min);
                {
                    c.If(count > 0);
                    {
                        c.Assign(o, m.Field("E").Invoke("Peek"));
                    }
                    c.Else();
                    {
                        c.Assign(o, s.Field("S"));
                    }
                    c.End();
                    //c.WriteLine("rep might return notd at " + o);
                    c.GotoFalse(returned, Notd);
                    //c.WriteLine("rep never mind");
                    c.Goto(Back);
                }
                c.End();
                //c.If(count <= 0);
                //c.WriteLine("rep zero matches; we're done here");
                //c.End();
                c.GotoFalse(count > 0, Done);
                c.Assign(o, m.Field("E").Invoke("Peek"));
                //c.WriteLine("rep returning done at " + o);
                c.Goto(Done);
            }
            else
            {
                c.Label(Init);
                c.Assign(s, s.Invoke("D", o));
                c.Assign(v, (uint)0);
                c.Label(retry);

                //c.WriteLine("rep trying one");
                L.Emit(g);

                if (L.MinLength == 0)
                    c.Goto(Done); // zero-width assertion repetition...?
                c.Increment(v);
                c.Assign(s.Field("F"), v);

                if (Max == -1) {
                    c.Goto(retry);
                } else {
                    c.GotoTrue(v < Max, retry);
                    c.GotoTrue(v > Min, Notd);
                    c.Goto(Done);
                }

                c.Label(Back);
                c.Assign(v, s.Field("F") - 1);
                c.Assign(o, s.Field("S") + (v.Cast(typeof(int)) * L.MinLength));

                c.Label(L.Fail);
                c.If(v < Min);
                {
                    g.Ascend(c);
                    c.Assign(o, s.Field("S"));
                    c.Goto(Fail);
                }
                c.End();
                c.GotoTrue(v == Min, Done);
                c.Assign(s.Field("F"), v);
                c.Goto(Notd);

            }
        }

        public override int MinLength {
            get { return Min * L.MinLength; }
        }

        public override int MaxLength {
            get { return Max == -1 ? int.MaxValue : Max * L.MaxLength; }
        }

        public override Pattern Regen(Grammar g) {
            return new Repetition(L.Regen(g), Min, Max);
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(Repetition), L.EmitRegen(), Min, Max);
        }

        public override bool Backtracking {
            get {
                return L.Backtracking || Min != Max;
            }
        }
    }
}
