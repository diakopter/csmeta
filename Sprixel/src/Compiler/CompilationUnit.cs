// 
// CompilationUnit.cs
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

using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using TriAxis.RunSharp;
using TriAxis.RunSharp.Operands;

namespace Sprixel {
    using Routine = Recursive<CompilationUnit, int, int>;

    public delegate R Recursive<A, B, C, D, R>(Recursive<A, B, C, D, R> r, A a, B b, C c, D d);
    public delegate R Recursive<A, B, C, R>(Recursive<A, B, C, R> r, A a, B b, C c);
    public delegate R Recursive<A, B, R>(Recursive<A, B, R> r, A a, B b);
    public delegate R Recursive<A, R>(Recursive<A, R> r, A a);
    public delegate R Recursive<R>(Recursive<R> r);
    public delegate F Fix<F>(Func<F, F> f);

    public class CompilationUnit {
        public bool IsRoot;
        public CompilationUnit Invocant;
        public Routine EntryPoint;
        public Match Match;
        public Grammar InitialGrammar;
        public CodeGen C;
        public InputSource SourceCode;
        public bool Compiled;
        public List<IInstruction> I;

        public CompilationUnit() {
            I = new List<IInstruction>();
        }

        public DynamicMethodGen GetDMG() {
            return DynamicMethodGen.Static(typeof(CompilationUnit))
                .Method(typeof(int))
                .Parameter(typeof(Routine), "dm")
                .Parameter(typeof(CompilationUnit), "cunit")
                .Parameter(typeof(int), "i");
        }

        public Routine GetEntryPoint(DynamicMethodGen dmg) {
            var dm = dmg.GetCompletedDynamicMethod(true);
            return dm.CreateDelegate(typeof(Routine)).CastTo<Routine>();
        }

        public void Compile() {
            var method = this.GetDMG();
            var codegen = C = method.GetCode();
            foreach (var inst in I) inst.Emit(this);
            EntryPoint = this.GetEntryPoint(method);
            Compiled = true;
        }

        public int Invoke(CompilationUnit invocant) {
            Invocant = invocant;
            return DoInvoke();
        }

        public int Invoke() {
            IsRoot = true;
            return DoInvoke();
        }

        public int DoInvoke() {
            if (!Compiled)
                Compile();
            return EntryPoint.Invoke(EntryPoint, this, 1);
        }
    }

    public static partial class Extensions {
        public static T CastTo<T>(this object obj) {
            return (T)obj;
        }
    }
}
