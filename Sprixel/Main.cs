// 
// Main.cs
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
using System.Linq;
using TriAxis.RunSharp;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using TriAxis.RunSharp.Operands;
using System.Collections;


namespace Sprixel
{
    public delegate void EntryPoint();

    public class Invoker
    {
        public void WriteLine(object arg)
        {
            Console.WriteLine(arg.ToString());
        }
    }

    public class MainClass
    {
        public const bool BuildFirstStage = true;

        public const bool SaveStage2AssemblyToDisk = true;

        public const bool SaveStage1AssemblyToDisk = true;

        public const bool RunIndividualTest = false;

        public const string TestFileToRun = "grammar1.t";

        public const string settingString = "sub say(object $a-->int) {System::Console.WriteLine($a.ToString());return 1;};sub print(object $a-->int) {System::Console.Write($a.ToString()); return 1;};";

        public static void Main(string[] args)
        {
            //if ((args != null && args.Length > 0) || BuildPerlesqueOverride) {
            var largs = args.ToList();
            //if (largs.Contains("build0") || BuildPerlesqueOverride) {
            //var sw = new Stopwatch();
            //sw.Start();
            var g = Perlesque.BuildPerlesque("perlesque", SaveStage1AssemblyToDisk, SaveStage2AssemblyToDisk);
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
            UTF32String input;
            if (RunIndividualTest)
                input = new UTF32String(settingString + System.IO.File.ReadAllText(@"..\..\t\" + TestFileToRun));
            else if (largs.Count > 0) {
                if (largs.Count > 1 && args[0] == "-e" && args[1] != "") {
                    input = new UTF32String(settingString + args[1]);
                } else
                    input = new UTF32String(settingString + System.IO.File.ReadAllText(args[0]));
            } else
                input = new UTF32String(settingString +
""
);
            //sw.Reset();
            //sw.Start();
            var match2 = g.Parse(input);
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
            //sw.Reset();
            //sw.Start();
            if (!match2.Success)
                Console.WriteLine("parsefail");
            //for (var i = 10; --i > 0; ) {
            //    g.Parse(input);
            //}

            //Console.WriteLine(sw.Elapsed);
            //}
            //} else {
            //Blah4();
            //Blah7.Run();
            //}
        }
    }
}

