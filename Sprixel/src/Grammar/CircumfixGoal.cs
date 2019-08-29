// 
// CircumfixGoal.cs
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
    public class CircumfixGoalContainer {
        public CircumfixGoal Goal;
    }

    public class CircumfixGoal {
        public CircumfixGoal Parent;
        public UTF32String GoalText;

        public CircumfixGoal(string goalText, CircumfixGoal parent) {
            GoalText = new UTF32String(goalText);
            Parent = parent;
        }

        public int Match(uint[] input, int offset, CircumfixGoalContainer container) {
            var os = offset;
            var op = GoalText.Chars;
            if (op.Length + os > input.Length)
                return 0;
            foreach (var codepoint in op)
                if (input[offset++] != codepoint)
                    return 0;
            container.Goal = container.Goal.Parent; // pop the stack
            return offset - os;
        }
    }
}
