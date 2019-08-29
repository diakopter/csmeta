// 
// PatternCall.cs
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
    public class PatternCall : UnaryPattern, IBackTracking {
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public PatternCall(Pattern l)
            : base(l) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            IBackTracking l;

            c.Label(Init);
            L.Emit(g);

            if (L.Backtracking) {
                l = L as IBackTracking;

                c.Label(l.Done);
                c.Assign(c["i1"], c["call"].Field("Done"));
                c.Goto(g.SwitchLabel);

                c.Label(l.Notd);
                c.Assign(c["i1"], c["call"].Field("Notd"));
                c.Goto(g.SwitchLabel);

                c.Label(L.Fail);
                c.Assign(c["i1"], c["call"].Field("Fail"));
                c.Goto(g.SwitchLabel);

                c.Label(Back);
                c.Goto(l.Back);
            } else {
                c.Assign(c["i1"], c["call"].Field("Done"));
                c.Goto(g.SwitchLabel);
                
                c.Label(L.Fail);
                c.Assign(c["i1"], c["call"].Field("Fail"));
                c.Goto(g.SwitchLabel);

                c.Label(Back);
            }
        }

        public override Pattern Regen(Grammar g) {
            return new PatternCall(L.Regen(g));
        }

        public override bool Backtracking {
            get { return Recursive ? true : L.Backtracking; }
        }

        public override int MinLength { // TODO: fix this... not sure how...
            get { return Recursive ? 1 : L.MinLength; }
        }

        public override int MaxLength {
            get { return Recursive ? int.MaxValue : L.MaxLength; }
        }
    }
}
