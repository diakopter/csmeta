﻿// 
// Lookahead.cs
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
    public class Lookahead : UnaryPattern, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public Lookahead(Pattern l)
            : base(l) {
            if (l.Backtracking) {
                Done = Transition.Next;
                Notd = Transition.Next;
                Back = Transition.Next;
                Init = Transition.Next;
            }
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
                c.Label(l.Done);
                g.Ascend(c);
                c.Assign(o, s.Field("S"));
                c.Goto(Done);

                c.Label(Back); // unreachable (this never returns not done)
                c.Label(L.Fail);
                g.Ascend(c);
                c.Goto(Fail);
            } else {
                if (!c.NamedLocals.ContainsKey("loc_" + Fail)) {
                    var unused = c["loc_" + Fail, typeof(int)];
                }
                var done = Transition.Next;
                c.Assign(c["loc_" + Fail], o);
                L.Emit(g);

                c.Assign(o, c["loc_" + Fail]);
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
            return new Lookahead(L.Regen(g));
        }

        public override bool Backtracking {
            get {
                return L.Backtracking;
            }
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(Lookahead), L.EmitRegen());
        }
    }
}
