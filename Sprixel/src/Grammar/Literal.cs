// 
// Literal.cs
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
    public class Literal : UnitPattern {

        public const bool DebugLiteral = false;

        public string Target;
        public UTF32String UTF32Target;

        public Literal(string target) {
            Target = target;
            UTF32Target = new UTF32String(target);
        }

        public override void Emit(Grammar g) {
            var c = g.CodeGen;
            var utf32 = UTF32Target;
            if (DebugLiteral)
                c.WriteLine("literal trying '" + Target + "' at " + c["o"] + ", which is '" + c["i"][c["o"]].Cast(typeof(char)) + "'");
            c.GotoFalse(c["o"] + utf32.Length <= c["l"] && utf32.Chars[0] == c["i"][c["o"]], Fail);
            for (var i = 1; i < utf32.Length; i++)
                c.GotoFalse(Operand.FromObject(utf32.Chars[i]) == c["i"][c["o"] + i], Fail);
            c.AssignAdd(c["o"], utf32.Length);
        }

        public override Pattern Regen(Grammar g) {
            return new Literal(Target);
        }

        public override TriAxis.RunSharp.Operand EmitRegen() {
            return Exp.New(typeof(Literal), Target);
        }

        public override int MinLength {
            get { return UTF32Target.Length; }
        }

        public override int MaxLength {
            get { return UTF32Target.Length; }
        }

        public override string ToString() {
            return "Literal(" + Target + ")";
        }
    }
}
