// 
// Pattern.cs
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
using TriAxis.RunSharp;

namespace Sprixel {
    public abstract class Pattern {
        public bool Deterministic;
        public bool Recursive;
        public Transition Fail = Transition.Next;
        public HashSet<string> NamedRefs;

        public abstract bool Backtracking { get; }
        public abstract Pattern Regen(Grammar g);
        public abstract Operand EmitRegen();
        public abstract void Emit(Grammar g);
        public abstract int MinLength { get; }
        public abstract int MaxLength { get; }
        public abstract void ResolveSym(string symbol, Pattern parent, bool isRight);
        public abstract void ComputeRefs(Grammar g, string name, HashSet<string> refs);
        public virtual void Run(ref ParseEnv pe, ref Match m) {
        }
        public enum PState {
            Fail = 0,
            Done = 1,
            Notd = 2
        }
    }
}
