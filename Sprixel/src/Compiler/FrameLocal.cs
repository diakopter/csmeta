// 
// FrameLocal.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using TriAxis.RunSharp;

namespace Sprixel {
    public class FrameLocal {
        public static uint MangleCounter;
        public string Name;
        public string MangledName;
        public FrameScope Scope;
        public Type Type;
        public bool Invokable;
        public TypeGen TypeGen;

        public FrameLocal(string name, TypeGen type, FrameScope frameScope) {
            Type = TypeGen = type;
            Name = name;
            Scope = frameScope;
            MangledName = "l" + MangleCounter++;
            //Console.WriteLine("Added " + name + " as " + MangledName + " of type " + type + " to " + Scope.Typegen);
        } // (name.Contains("*") ? "_ctxl_" : "") + 

        public FrameLocal(string name, Type type, FrameScope frameScope) {
            Type = type;
            Name = name;
            Scope = frameScope;
            MangledName = "l" + MangleCounter++;
            //Console.WriteLine("Added " + name + " as " + MangledName + " of type " + type + " to " + Scope.Typegen);
        }

        public Type GetReturnType() { // for IClosure<> only
            return Type.GetGenericArguments()[0];
        }
    }
}
