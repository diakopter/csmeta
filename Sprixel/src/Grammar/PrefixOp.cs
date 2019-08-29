// 
// PrefixOp.cs
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
    public class PrefixOp : UnitPattern {

        public PrefixOp() {
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            c.Assign(c["i2"], c["GG"].Field("PrefixOperators").Invoke("Match", c["i"], c["o"], c["pp"]));
            c.GotoTrue(c["i2"] == 0, Fail);
            c.AssignAdd(c["o"], c["i2"]);
        }

        public override Pattern Regen(Grammar g) {
            return new PrefixOp();
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(PrefixOp));
        }

        public override bool Backtracking { get { return false; } }

        public override int MinLength { get { return 1; } }

        public override int MaxLength { get { return int.MaxValue; } }
    }
}
