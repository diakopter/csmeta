// 
// NamedGroup.cs
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
    public class NamedGroup : UnaryPattern, IBackTracking {
        public string Name;
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public NamedGroup(string name, Pattern l)
            : base(l) {
            if (Backtracking) {
                Done = Transition.Next;
                Notd = Transition.Next;
                Back = Transition.Next;
                Init = Transition.Next;
            }
            Name = name;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var m = c["m"];
            Transition skip;
            IBackTracking l;

            if (L.Backtracking)
                c.Label(Init);
            else {
                c.Goto(skip = Transition.Next);
                
                c.Label(L.Fail);
                c.Assign(m, m.Invoke("MParent"));
                c.Goto(Fail);

                c.Label(skip);
            }

            //c.WriteLine("Trying a " + Name + " at " + c["o"]);
            // push another match onto the match stack
            c.Assign(m, Exp.New(typeof(Match), Name, m, c["o"]));

            L.Emit(g);
            //c.WriteLine("nonbackt NamedGroup " + Name + " matched from " + m.Field("Start") + " to " + c["o"] + ". now we're back to " + m.Invoke("MParent").Field("Name"));

            if (L.Backtracking) {
                l = L as IBackTracking;
                
            //c.WriteLine("nonbackt NamedGroup " + Name + " fell through to Init!");
                c.Label(l.Done);
                //c.WriteLine("NamedGroup " + Name + " matched from " + m.Field("Start") + " to " + c["o"] + ". now we're back to " + m.Invoke("MParent").Field("Name"));
                c.Assign(m, m.Invoke("MParent", c["o"]));
                c.Goto(Done);

                c.Label(l.Notd);
                //c.WriteLine("NamedGroup " + Name + " matched from " + m.Field("Start") + " to " + c["o"] + ". now we're back to " + m.Invoke("MParent").Field("Name"));
                c.Assign(m, m.Invoke("MParent", c["o"]));
                c.Goto(Notd);

                c.Label(L.Fail);
                c.Assign(m, m.Invoke("MParent"));
                c.Goto(Fail);

                c.Label(Back);
                c.Assign(m, Exp.New(typeof(Match), Name, m, c["o"]));
                c.Goto(l.Back);
            } else {
                c.Assign(m, m.Invoke("MParent", c["o"]));
            }
        }

        public override Pattern Regen(Grammar g) {
            return new NamedGroup(Name, L.Regen(g));
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(NamedGroup), Name, L.EmitRegen());
        }
    }
}
