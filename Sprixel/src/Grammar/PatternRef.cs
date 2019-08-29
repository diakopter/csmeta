// 
// PatternRef.cs
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
    public class PatternRef : UnitPattern, IBackTracking {

        public const bool DebugPatternRef = false;

        public string Name;
        public bool RefsComputed;
        public Pattern Target;
        public Transition Done { get; set; }
        public Transition Notd { get; set; }
        public Transition Back { get; set; }
        public Transition Init { get; set; }

        public PatternRef(string name) {
            Done = Transition.Next;
            Notd = Transition.Next;
            Back = Transition.Next;
            Init = Transition.Next;
            Name = name;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var s = c["s"];
            var o = c["o"];
            var call = c["call"];
            var done = Transition.Next;
            var notd = Transition.Next;
            var fail = Transition.Next;
            var pattern = g[Name];
            var bt = pattern.Backtracking;


            var newRefCall = Exp.New(typeof(RefCall), (uint)done, (uint)notd, (uint)fail, c["call"]);
            var pat = pattern as IBackTracking;

            c.Label(Init);
            if (DebugPatternRef)
                c.WriteLine(Name + " init at " + o + " id: " + s.Field("id"));
            c.Assign(call, newRefCall);
            if (!bt)
                c.Assign(s, s.Invoke("D", o));
            //c.WriteLine(s.Field("id"));
            c.Goto(pat.Init);

            c.Label(Back);
            if (DebugPatternRef)
                c.WriteLine(Name + " back at " + o);
            if (!bt)
                c.Assign(s, s.Invoke("D", o));
            //c.WriteLine(s.Field("id"));
            c.Assign(call, newRefCall);
            c.Goto(pat.Back);

            c.Label(fail);
            if (DebugPatternRef)
                c.WriteLine(Name + " --- fail at " + o);
            if (!bt)
                g.Ascend(c);
            //c.WriteLine(s.Field("id"));
            c.Goto(Fail);

            c.Label(notd);
            if (DebugPatternRef)
                c.WriteLine(Name + " ___ notd at " + o);
            //if (!bt)
            //    g.Ascend(c);
            //c.WriteLine(s.Field("id"));
            c.Goto(Notd);

            c.Label(done);
            if (DebugPatternRef)
                c.WriteLine(Name + " +++ done at " + o);
            if (!bt) {
                //g.Ascend(c);
                //c.WriteLine(s.Field("id"));
                c.Goto(Done);
            } else {
                //c.WriteLine(s.Field("id"));
            }
        }

        public override Pattern Regen(Grammar g) {
            return new PatternRef(Name);
            var r = new PatternRef(Name);
            if (r.RefsComputed = RefsComputed)
            r.Target = g[Name];
            return r;
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(PatternRef), Name);
        }

        public override bool Backtracking {
            get {
                return true; // yes, all named refs must act as if they're backtracking. :|
            }
        }

        public override int MinLength {
            get {
                return RefsComputed
                    ? Target.MinLength
                    : 0;
            }
        }

        public override int MaxLength {
            get {
                return RefsComputed
                    ? Target.MaxLength
                    : Int32.MaxValue;
            }
        }

        public override void ComputeRefs(Grammar g, string name, HashSet<string> refs) {
            var hasIt = refs.Contains(Name);
            if (!g.ContainsKey(Name))
                throw new InvalidOperationException("pattern " + Name + " has not been declared");
            refs.Add(Name);
            RefsComputed = true;
            Target = g[Name];
            if (!hasIt && name != Name) // prevent cyclical recursion
                Target.ComputeRefs(g, name, refs);
        }
    }
}
