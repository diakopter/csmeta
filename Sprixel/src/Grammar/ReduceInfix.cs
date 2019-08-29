// 
// ReduceInfix.cs
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
    public class ReduceInfix : UnaryPattern, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }
        public int Counter;

        public ReduceInfix(Pattern l)
            : base(l) {
            if (l.Backtracking) {
                Done = Transition.Next;
                Notd = Transition.Next;
                Back = Transition.Next;
                Init = Transition.Next;
            }
        }

        void EmitAction(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var o = c["o"];
            var m = c["m"];
            var endmarker = Init + "ReduceInfix" + Counter++;

            var found = c["IN"].Invoke("Match", m["InfixOp", 1]);
            var left = m["LeftOperand", 1];
            var right = m["RightOperand", 1];
            //c.WriteLine(m.Invoke("Matches", c["IN"]));
            c.GotoFalse(left.IsInstanceOf(typeof(object)), endmarker);
            c.WriteLine("dispatching left " + c["IN"].Invoke("Match", left) + ": " + left.Field("Start") + " to " + left.Field("End") + " " + left.Field("Name"));
            //c.WriteLine(left.Invoke("Matches", c["IN"]));
            c.If(right.IsInstanceOf(typeof(object)).LogicalNot());
            {
                //c.Assign(c["m"].Field("Result"), c["m"]["LeftOperand", 1].Field("Result"));
                //c.Invoke(c["m"], "Pop", "LeftOperand");
                c.Goto(endmarker);
            }
            c.End();
            c.WriteLine("dispatching right " + c["IN"].Invoke("Match", right) + ": " + right.Field("Start") + " to " + right.Field("End") + " " + right.Field("Name"));
            //c.WriteLine(right.Invoke("Matches", c["IN"]));
            /*c.Try();
            {
                c.WriteLine("Result is " + right.Field("Result"));
            }
            c.CatchAll();
            c.Try();
            {
                c.WriteLine("Parent Result is " + right.Invoke("MParent").Field("Result"));
            }
            c.CatchAll();
            c.Goto(endmarker);
            c.End();
            c.End();*/
            c.GotoFalse(found.IsInstanceOf(typeof(object)), endmarker);

            //c.WriteLine("dispatching found '" + found + "'");
            c.Switch(found);

            foreach (var target in g.InfixOperators.Keys) {
                var op = g.InfixOperators[target];
                if (op.ReduceActionGen != null) {
                    c.Case(target);
                    c.WriteLine("dispatching infix " + target + "\n");
                    op.ReduceActionGen(g, target, left, right);
                }
            }
            c.End();
            c.Label(endmarker);
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var o = c["o"];

            if (L.Backtracking) {
                var l = L as IBackTracking;
                c.Label(Init);
                c.Assign(s, s.Invoke("D", o));
                L.Emit(g);

                c.Label(l.Notd);
                EmitAction(g);
                c.Assign(s, s.Property("TLI"));
                c.Goto(Notd);

                c.Label(l.Done);
                EmitAction(g);
                g.Ascend(c);
                c.Goto(Done);

                c.Label(Back);
                c.Assign(s, s.Field("L"));
                c.Goto(l.Back);

                c.Label(L.Fail);
                g.Ascend(c);
                c.Goto(Fail);
            } else {
                var done = Transition.Next;
                L.Emit(g);

                EmitAction(g);
                c.Goto(done);

                c.Label(L.Fail);
                c.Goto(Fail);

                c.Label(done);
            }
        }

        public override int MinLength {
            get { return 0; }
        }

        public override int MaxLength {
            get { return 0; }
        }

        public override Pattern Regen(Grammar g) {
            return new ReduceInfix(L.Regen(g));
        }

        public override bool Backtracking {
            get {
                return L.Backtracking;
            }
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            throw new NotImplementedException();
            //return Exp.New(typeof(ReduceAction), L.EmitRegen());
        }
    }
}
