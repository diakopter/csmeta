// 
// RangeChar.cs
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
    public class RangeChar : UnitPattern {

        public const bool DebugRangeChar = false;

        uint Lo;
        uint Hi;

        public RangeChar(uint lo, uint hi) {
            Lo = lo;
            Hi = hi;
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var offset = c["o"];
            var input = c["i"];
            var local1 = c["i1"];
            var length = c["l"];
            if (DebugRangeChar)
                c.WriteLine("rangechar trying '" + (char)Lo + "' to '" + (char)Hi + "' at " + c["o"] + ", which is '" + c["i"][c["o"]].Cast(typeof(char)) + "'");
            c.GotoTrue(offset >= length || (local1 = input[offset]) < Lo || local1 > Hi, Fail);
            c.Increment(offset);
        }

        public override Pattern Regen(Grammar g) {
            return new RangeChar(Lo, Hi);
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(RangeChar), Lo, Hi);
        }

        public override int MinLength {
            get { return 1; }
        }

        public override int MaxLength {
            get { return 1; }
        }
    }
}
