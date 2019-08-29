// 
// CircumfixOp.cs
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
    public class CircumfixOp : UnaryPattern, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public CircumfixOp(Pattern pattern)
            : base(pattern) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var o = c["o"];
            IBackTracking l;
            if (L.Backtracking) {
                l = L as IBackTracking;

                c.Label(Init);
                //c.WriteLine("CircumfixOp init at " + o);

                c.Assign(c["i2"], c["GG"].Field("CircumfixOperators").Invoke("Match", c["i"], c["o"], c["pp"], c["cgc"]));
                c.GotoTrue(c["i2"] == 0, Fail);
                c.AssignAdd(c["o"], c["i2"]);

                //c.WriteLine(s.Field("id"));
                c.Assign(s, s.Invoke("D", o));
                //c.WriteLine(s.Field("id"));

                L.Emit(g);

                c.Label(l.Done);
                //c.WriteLine("CircumfixOp done at " + o);
                //c.WriteLine(s.Field("id"));
                g.Ascend(c);
                //c.WriteLine(s.Field("id"));
                //c.WriteLine(c["cgc"].Field("Goal"));
                c.Assign(c["i2"], c["cgc"].Field("Goal").Invoke("Match", c["i"], c["o"], c["cgc"]));
                c.GotoTrue(c["i2"] == 0, L.Fail);
                c.AssignAdd(c["o"], c["i2"]);
                c.Goto(Done);

                c.Label(l.Notd);
                //c.WriteLine("CircumfixOp notd at " + o);
                //c.WriteLine(s.Field("id"));
                c.Assign(s, s.Property("TLI"));
                //c.WriteLine(s.Field("id"));
                c.Assign(c["i2"], c["cgc"].Field("Goal").Invoke("Match", c["i"], c["o"], c["cgc"]));
                c.GotoTrue(c["i2"] == 0, L.Fail);
                c.AssignAdd(c["o"], c["i2"]);
                c.Goto(Notd);

                c.Label(L.Fail);
                //c.WriteLine("CircumfixOp fail at " + o);
                //c.WriteLine(s.Field("id"));
                g.Ascend(c);
                //c.WriteLine(s.Field("id"));
                c.Goto(Fail);

                c.Label(Back);
                //c.WriteLine("CircumfixOp back at " + o);
                //c.WriteLine(s.Field("id"));
                c.Assign(c["i2"], c["GG"].Field("CircumfixOperators").Invoke("Match", c["i"], c["o"], c["pp"], c["cgc"]));
                c.Assign(s, s.Field("L"));
                c.Goto(l.Back);

            } else {
                c.Assign(c["i2"], c["GG"].Field("CircumfixOperators").Invoke("Match", c["i"], c["o"], c["pp"], c["cgc"]));
                c.GotoTrue(c["i2"] == 0, Fail);
                c.AssignAdd(c["o"], c["i2"]);

                L.Fail = Fail;

                L.Emit(g);

                c.Assign(c["i2"], c["cgc"].Field("Goal").Invoke("Match", c["i"], c["o"], c["cgc"]));
                c.GotoTrue(c["i2"] == 0, Fail);
                c.AssignAdd(c["o"], c["i2"]);
            }
        }

        public override Pattern Regen(Grammar g) {
            return new CircumfixOp(L.Regen(g));
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(CircumfixOp), L.EmitRegen());
        }

        public override bool Backtracking { get { return L.Backtracking; } }

        public override int MinLength { get { return 2; } }

        public override int MaxLength { get { return int.MaxValue; } }
    }
}
