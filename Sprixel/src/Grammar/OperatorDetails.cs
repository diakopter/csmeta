// 
// OperatorDetails.cs
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

    public class OperatorDetails {
        public AssociativityType Associativity;
        public string PrecedenceLevel;
        public string Goal;
        public Action<Grammar, string, Operand, Operand> ReduceActionGen;

        public OperatorDetails(string precedenceLevel, AssociativityType associativityType, Action<Grammar, string, Operand, Operand> ag) {
            Associativity = associativityType;
            PrecedenceLevel = precedenceLevel;
            Goal = "";
            ReduceActionGen = ag;
        }
        public OperatorDetails(string precedenceLevel, AssociativityType associativityType, Action<Grammar, string, Operand, Operand> ag, string closer) {
            Associativity = associativityType;
            PrecedenceLevel = precedenceLevel;
            Goal = closer;
            ReduceActionGen = ag;
        }
    }

    public class OperatorSet : PrototypeChain<string, OperatorDetails> {
        public OperatorSet()
            : base() {
        }
        public OperatorSet(OperatorSet parent)
            : base(parent) {
        }

        Dictionary<string, uint[][]> _cachedLTS = new Dictionary<string, uint[][]>();

        public uint[][] LongestToShortest() {
            return LongestToShortest("_");
        }

        public uint[][] LongestToShortest(string precedenceLevel) {
            uint[][] res;
            if (_cachedLTS.TryGetValue(precedenceLevel, out res))
                return res;
            var sorted = from s in Keys
                         where this[s].PrecedenceLevel.CompareTo(precedenceLevel) >= 0
                         orderby s.Length descending
                         select new UTF32String(s).Chars;
            var arr = sorted.ToArray();
            _cachedLTS.Add(precedenceLevel, arr);
            return arr;
        }

        public int Match(uint[] input, int offset, string precedenceLevel) {
            var ops = LongestToShortest(precedenceLevel);
            var os = offset;
            foreach (var op in ops) {
                if (op.Length + os > input.Length)
                    goto next;
                foreach (var codepoint in op)
                    if (input[offset++] != codepoint)
                        goto next;
                return offset - os;
            next:
                offset = os;
            }
            return 0;
        }

        public int Match(uint[] input, int offset, string precedenceLevel, CircumfixGoalContainer container) {
            var ops = LongestToShortest(precedenceLevel);
            var os = offset;
            foreach (var op in ops) {
                if (op.Length + os > input.Length)
                    goto next;
                foreach (var codepoint in op)
                    if (input[offset++] != codepoint)
                        goto next;
                container.Goal = new CircumfixGoal(this[new UTF32String(op).ToString()].Goal, container.Goal);
                return offset - os;
            next:
                offset = os;
            }
            return 0;
        }
    }

    [Flags]
    public enum AssociativityType {
        Unary = 0x0000,
        Left = 0x0001,
        Right = 0x0002
    }
}
