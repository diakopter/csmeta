// 
// Grammar.cs
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
using TriAxis.RunSharp;
using System.Linq;
using Sprixel.Runtime;
using TriAxis.RunSharp.Operands;

namespace Sprixel {
    using ParserRoutine = Func<Matcher, UTF32String, int, uint, State, Match>;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

    public class Grammar : PrototypeChain<string, Pattern> {
        public static string SystemAssemblyName;

        public string Name;
        public ProtoPatternSet Protos;
        public CodeGen CodeGen;
        public Matcher Matcher;
        public bool Compiled;
        public Pattern TOP;
        public Transition SwitchLabel = Transition.Next;
        public OperatorSet PrefixOperators;
        public OperatorSet PostfixOperators;
        public OperatorSet InfixOperators;
        public OperatorSet CircumfixOperators;
        public OperatorSet PostCircumfixOperators;
        public Stack<CodeGen> CodeGens = new Stack<CodeGen>();
        public Stack<TypeGen> TypeGens = new Stack<TypeGen>();
        public static int FrameCounter;
        public int EphemeralCounter;
        public string ParseFailLabel;
        public Transition DoneLabel;

        public void Ascend(CodeGen c) {
            var s = c["s"];
            c.Assign(s, s.Field("I"));
        }

        public static void LoadSystemDll() {
            try {
                SystemAssemblyName = AssemblyName.GetAssemblyName(Regex.Replace(Regex.Replace(typeof(object).Assembly.CodeBase, "mscorlib.dll", "System.dll"), "file:///", "")).ToString();

            } catch (Exception e) {
                SystemAssemblyName = AssemblyName.GetAssemblyName("/usr/local/lib/mono/2.0/System.dll").ToString();
            }
            if (SystemAssemblyName != null) {
                Type.GetType("System.URI, " + SystemAssemblyName, false, true);
            }
        }

        public Grammar(string name, Grammar parent)
            : base(parent) {
            Name = name;
            Protos = new ProtoPatternSet(parent.Protos);
            PrefixOperators = new OperatorSet(parent.PrefixOperators);
            PostfixOperators = new OperatorSet(parent.PostfixOperators);
            InfixOperators = new OperatorSet(parent.InfixOperators);
            CircumfixOperators = new OperatorSet(parent.CircumfixOperators);
            PostCircumfixOperators = new OperatorSet(parent.PostCircumfixOperators);
            LoadSystemDll();
        }

        public Grammar(string name, Pattern toplevel)
            : this(name) {
            AddPattern("__toplevel", TOP = toplevel);
            LoadSystemDll();
        }

        public Grammar(string name)
            : base() {
            Name = name;
            Protos = new ProtoPatternSet();
            PrefixOperators = new OperatorSet();
            PostfixOperators = new OperatorSet();
            InfixOperators = new OperatorSet();
            CircumfixOperators = new OperatorSet();
            PostCircumfixOperators = new OperatorSet();
            LoadSystemDll();
        }

        public Operand[] BindArgs(int flag, Match match)
        {
            var omatch = match;
            var args = new List<Operand>();
            while (match != null && match.HasOwnKey("argsList") && match["argsList"].HasOwnKey("nextArg"))
            {
                var arg = match["argsList"]["nextArg"];
                if (null != (object)arg.Result)
                {
                    args.Add(arg.Result);
                    //Console.WriteLine("arg " + (args.Count - 1) + " is a " + arg.Result.Type);
                }
                else
                    break;
                match = match["argsList"];
            }
            return args.ToArray();
        }

        public Operand[] BindArgs(CodeGen cg, Match match) {
            var omatch = match;
            var args = new List<Operand>();
            args.Add(cg.This());
            while (match != null && match.HasOwnKey("argsList") && match["argsList"].HasOwnKey("nextArg")) {
                var arg = match["argsList"]["nextArg"];
                if (null != (object)arg.Result)
                {
                    args.Add(arg.Result);
                    //Console.WriteLine("arg " + (args.Count - 1) + " is a " + arg.Result.Type);
                }
                else
                    break;
                match = match["argsList"];
            }
            return args.ToArray();
        }

        public Dictionary<string, TypeGen> ClosuresByInterface = new Dictionary<string, TypeGen>();
        public Dictionary<string, TypeGen> ClosuresByName = new Dictionary<string, TypeGen>();
        public Dictionary<string, TypeGen> ClassesByName = new Dictionary<string, TypeGen>();
        public Dictionary<string, TypeGen> ReturnInterfaces = new Dictionary<string, TypeGen>();
        public Dictionary<string, Type> ReturnTypesByInterface = new Dictionary<string, Type>();
        public static Dictionary<string, Type> GenericTypeDefinitionsByGenericTypeNames = new Dictionary<string, Type>();
        
        // construct a class for each subroutine to store the locals and a link to the outer frame
        public TypeGen DescendIntoClass(AssemblyGen ag, string className) {
            var classtg = ag.Public.Class("csmeta_" + className);

            return classtg;
        }

        internal struct Param {
            public Type Type;
            public string Name;
            public bool IsTypeGen;
            public Param(Type type, string name, bool isTypeGen) {
                Type = type;
                Name = name;
                IsTypeGen = isTypeGen;
            }
        }

        // This is the primary frame-builder routine.

        // construct a class for each subroutine to store the locals and a link to the outer frame
        public TypeGen DescendIntoSub(AssemblyGen ag, TypeGen tg, TypeGen topLevelTypeGen, FrameScope frameScope, UTF32String inputString, Match subNameMatch, Match returnTypeMatch, Match paramsListMatch) {
            Type returnType;
            string sigstr;
            if ((returnTypeMatch.Result as object) != null && returnTypeMatch.Success && returnTypeMatch.Result is TypeLiteral) {
                returnType = (returnTypeMatch.Result as TypeLiteral).ConstantValue as Type;
                sigstr = returnType.FullName;
            } else {
                returnType = ResolveType(sigstr = inputString.Match(returnTypeMatch));
            }
            var returnInterfaceName = "IReturn_" + returnType.FullName.Replace(".", "Dot").Replace("`", "Backtick");
            var origtg = tg;
            TypeGens.Push(tg);
            // create the interface for this subroutine's scope closure
            int counter = ++FrameCounter;
            
            TypeGen intf;
            TypeGen retIntf;
            MethodGen intf_binder;
            MethodGen intf_returner;

            var paramList = new List<Param>();

            while (paramsListMatch != null && paramsListMatch.HasOwnKey("paramsList") && paramsListMatch.Success && paramsListMatch["paramsList"].HasOwnKey("nextParam") && paramsListMatch["paramsList"]["nextParam"].Success)
            {
                var arg = paramsListMatch["paramsList"]["nextParam"];
                var argName = inputString.Match(arg["myVarName"]);
                paramList.Add(new Param((arg["myTypeName"].Result as TypeLiteral).ConstantValue as Type, argName, ((arg["myTypeName"].Result as TypeLiteral).ConstantValue as Type).Name.StartsWith("_IClosure")));
                paramsListMatch = paramsListMatch["paramsList"];
            }
            foreach (var param in paramList)
                sigstr += "|" + param.Type.FullName;

            if (!ClosuresByInterface.TryGetValue(sigstr, out intf)) {
                ReturnTypesByInterface.Add("_IClosure_" + counter, returnType);
                //Console.WriteLine("Added interface " + "_IClosure_" + counter + " with return type " + returnType);
                intf = ag.Public.Interface("_IClosure_" + counter);
                ClosuresByName.Add("_IClosure_" + counter, intf);
                ClassesByName.Add("_IClosure_" + counter, intf);
                intf_binder = intf.Method(FrameBaseGen, "Bind")
                    .Parameter(FrameBaseGen, "caller");

                foreach (var param in paramList)
                    intf_binder.Parameter(param.Type, param.Name);

                //Console.WriteLine("AddingDescend " + sigstr);
                ClosuresByInterface.Add(sigstr, intf);
            }

            //intf.Complete();
            var newtg = ag.Public.Sealed.Class("_Closure_" + counter, typeof(object), intf);
            ClosuresByName.Add("_Closure_" + counter, newtg);

            if (!ReturnInterfaces.TryGetValue(returnInterfaceName, out retIntf)) {
                retIntf = ag.Public.Interface(returnInterfaceName);
                intf_returner = retIntf.Method(returnType, "GetReturnValue");
                ReturnInterfaces.Add(returnInterfaceName, retIntf);
                //retIntf.Complete();
            }

            //newtg.Constructor(); 
            var closure_method = newtg.Constructor().Parameter(origtg.FrameGen, origtg.FrameGen.Name);
            CodeGen cg4 = closure_method;

            var frame = ag.Public.Sealed.Class("_Frame_" + counter, FrameBaseGen, retIntf);

            frame.ReturnIntGen = retIntf;
            //Console.WriteLine("set return interface of " + ((Type)newtg).Name + " to " + retIntf);
            newtg.FrameGen = frame;
            newtg.IntGen = intf;
            frame.Public.Field(newtg, "_Closure_" + counter);
            frame.Public.Field(returnType, "Return");
            MethodGen ret_method = frame.Method(returnType, "GetReturnValue");
            {
                CodeGen g = ret_method;
                g.Return(g.This().Field("Return"));
            }

            var newFrameScope = new FrameScope(newtg, frameScope);
            newtg.FrameScope = newFrameScope;

            newtg.ParentScope = tg;

            // the actual closure link (frame being captured)
            newtg.Public.Field(origtg.FrameGen, origtg.FrameGen.Name);
            while (tg is object) { // Add a link slot to each of its parent scopes
                var framegen = tg.FrameGen;
                //newtg.Public.Field(framegen, framegen.Name);
                frame.Public.Field(framegen, framegen.Name);
                tg = tg.ParentScope;
            }

            CodeGen clonecg = frame.Public.Override.Method(FrameBaseGen, "Clone");
            var unused3 = clonecg["clone", frame];
            clonecg.Assign(clonecg["clone"], Exp.New(frame));
            clonecg.Assign(clonecg["clone"].Field(origtg.FrameGen.Name), clonecg.This().Field(origtg.FrameGen.Name));
            clonecg.Assign(clonecg["clone"].Field("Instruction"), clonecg.This().Field("Instruction"));
            clonecg.Assign(clonecg["clone"].Field("Caller"), clonecg.This().Field("Caller"));
            clonecg.Assign(clonecg["clone"].Field("Callee"), clonecg.This().Field("Callee"));
            frame.CloneGen = clonecg;
            //clonecg.Return(clonecg["clone"]);

            {

                var binder_method = newtg.MethodImplementation(intf, FrameBaseGen, "Bind")
                    .Parameter(FrameBaseGen, "caller");

                 foreach (var param in paramList)
                    binder_method.Parameter(param.Type, param.Name);

                CodeGen binder = binder_method;
                var unused = binder["frame", frame];
                binder.Assign(binder["frame"], Exp.New(frame));
                if (origtg.Name != "TopLevelFrame") {
                    binder.Assign(binder["frame"].Field("TopLevelFrame"), binder.This().Field(origtg.FrameGen.Name).Field("TopLevelFrame"));
                } else {
                    binder.Assign(binder["frame"].Field("TopLevelFrame"), binder.This().Field("TopLevelFrame"));
                }
                //binder.WriteLine("assigned TopLevelFrame in " + newtg.Name + " as " + binder["frame"].Field("TopLevelFrame"));
                binder.Assign(binder["frame"].Field("_Closure_" + counter), binder.This());
                clonecg.Assign(clonecg["clone"].Field("_Closure_" + counter), clonecg.This().Field("_Closure_" + counter));
                tg = origtg;
                var first = true;
                while (tg is object && tg.Name != "TopLevelFrame") { // Add a link to each of its parent scopes
                    if (first || origtg != tg) {
                        first = false;
                        var framegen = tg.FrameGen;
                    //Console.WriteLine("adding link to " + tg.Name + " in binder of " + newtg);
                        //binder.WriteLine("adding link to: " + tg.Name);
                        clonecg.Assign(clonecg["clone"].Field(framegen.Name), clonecg.This().Field(framegen.Name));
                        binder.Assign(binder["frame"].Field(framegen.Name), binder.This().Field(framegen.Name));
                        //binder.WriteLine("assigned a field from the object: " + binder.This().Field(tg.Name));
                        //binder.WriteLine("assigned it the value: " + binder["frame"].Field(tg.Name));
                    }
                    tg = tg.ParentScope;
                }

                foreach (var param in paramList) {
                    var type = param.Type;
                    var name = param.Name;
                    FrameLocal local;
                    local = new FrameLocal(name, param.Type, newFrameScope);
                    newFrameScope.AddHere(name, local);
                    frame.Public.Field(param.Type, local.MangledName);
                    local.Invokable = true;
                    //newtg.Public.Field(type, local.MangledName);
                    binder.Assign(binder["frame"].Field(local.MangledName), binder[name]);
                    clonecg.Assign(clonecg["clone"].Field(local.MangledName), clonecg.This().Field(local.MangledName));
                }

                binder.Assign(binder["frame"].Field("Caller"), binder.Arg("caller"));
                binder.Return(binder["frame"]);
                binder.Complete();
            }
            cg4.Assign(cg4.This().Field(origtg.FrameGen.Name), cg4.Arg(origtg.FrameGen.Name));

            string subName;
            if (subNameMatch != null && subNameMatch.Success) { // it's a named sub, so declare it as such, in case it's referenced internally.
                subName = inputString.Match(subNameMatch);
            } else {
                subName = "l" + (++FrameScope.Counter).ToString();
            }
            var local2 = new FrameLocal(subName, intf, frameScope);
            local2.Invokable = true;
            frameScope.AddHere(subName, local2);
            //Console.WriteLine("Declared sub " + subName);
            origtg.FrameGen.Public.Field(intf, local2.MangledName);
            var ccg = origtg.FrameGen.CloneGen;
            ccg.Assign(ccg["clone"].Field(local2.MangledName), ccg.This().Field(local2.MangledName));
            newtg.NamedSubMangledName = local2.MangledName;
            ClassesByName.Add(newtg.Name, newtg);
            return newtg;
        }

        public TypeGen DescendIntoMethod(AssemblyGen ag, TypeGen tg, TypeGen topLevelTypeGen, FrameScope frameScope, UTF32String inputString, Match subNameMatch, Match returnTypeMatch, Match paramsListMatch, TypeGen currentClassGen) {
            Type returnType;
            string sigstr;
            if (returnTypeMatch == null && inputString.Match(subNameMatch) == "new")
            {
                returnType = currentClassGen;
                sigstr = returnType.FullName;
            } else if ((returnTypeMatch.Result as object) != null && returnTypeMatch.Success && returnTypeMatch.Result is TypeLiteral) {
                returnType = (returnTypeMatch.Result as TypeLiteral).t;
                sigstr = returnType.FullName;
            } else {
                returnType = ResolveType(sigstr = inputString.Match(returnTypeMatch));
            }
            var returnInterfaceName = "IReturn_" + returnType.FullName.Replace(".", "Dot").Replace("`", "Backtick");
            var origtg = tg;
            TypeGens.Push(tg);
            // create the interface for this subroutine's scope closure
            int counter = ++FrameCounter;

            TypeGen intf;
            TypeGen retIntf;
            MethodGen intf_binder;
            MethodGen intf_returner;

            var paramList = new List<Param>();

            while (paramsListMatch != null && paramsListMatch.HasOwnKey("paramsList") && paramsListMatch.Success && paramsListMatch["paramsList"].HasOwnKey("nextParam") && paramsListMatch["paramsList"]["nextParam"].Success) {
                var arg = paramsListMatch["paramsList"]["nextParam"];
                var argName = inputString.Match(arg["myVarName"]);
                paramList.Add(new Param((arg["myTypeName"].Result as TypeLiteral).ConstantValue as Type, argName, ((arg["myTypeName"].Result as TypeLiteral).ConstantValue as Type).Name.StartsWith("_IClosure")));
                paramsListMatch = paramsListMatch["paramsList"];
            }
            foreach (var param in paramList)
                sigstr += "|" + param.Type.FullName;

            ReturnTypesByInterface.Add("_IClosure_" + counter, returnType);
            //Console.WriteLine("Added interface " + "_IClosure_" + counter + " with return type " + returnType);
            intf = ag.Public.Interface("_IClosure_" + counter);
            ClosuresByName.Add("_IClosure_" + counter, intf);
            ClassesByName.Add("_IClosure_" + counter, intf);
            intf_binder = intf.Method(FrameBaseGen, "Bind")
                .Parameter(FrameBaseGen, "caller"); // this will always be null when called from non-stackless code

            intf_binder.Parameter(currentClassGen, "__csmeta_invocant");

            foreach (var param in paramList)
                intf_binder.Parameter(param.Type, param.Name);

            //intf.Complete();
            var newtg = ag.Public.Sealed.Class("_Closure_" + counter, typeof(object), intf);
            ClosuresByName.Add("_Closure_" + counter, newtg);

            if (!ReturnInterfaces.TryGetValue(returnInterfaceName, out retIntf)) {
                retIntf = ag.Public.Interface(returnInterfaceName);
                intf_returner = retIntf.Method(returnType, "GetReturnValue");
                ReturnInterfaces.Add(returnInterfaceName, retIntf);
                //retIntf.Complete();
            }

            // the method's scope-closure.  assigned at "class declaration time" at runtime
            currentClassGen.Public.Static.Field(newtg, "_Closure_" + counter);

            //newtg.Constructor(); 
            var closure_method = newtg.Constructor().Parameter(origtg.FrameGen, origtg.FrameGen.Name);
            CodeGen cg4 = closure_method;

            var frame = ag.Public.Sealed.Class("_Frame_" + counter, FrameBaseGen, retIntf);

            // a "local" to represent the arg
            frame.Public.Field(currentClassGen, "__csmeta_invocant");

            frame.ReturnIntGen = retIntf;
            //Console.WriteLine("set return interface of " + ((Type)newtg).Name + " to " + retIntf);
            newtg.FrameGen = frame;
            newtg.IntGen = intf;
            frame.Public.Field(newtg, "_Closure_" + counter);
            frame.Public.Field(returnType, "Return");
            MethodGen ret_method = frame.Method(returnType, "GetReturnValue");
            {
                CodeGen g = ret_method;
                g.Return(g.This().Field("Return"));
            }

            var newFrameScope = new FrameScope(newtg, frameScope);
            newtg.FrameScope = newFrameScope;

            newtg.ParentScope = tg;

            // the actual closure link (frame being captured)
            newtg.Public.Field(origtg.FrameGen, origtg.FrameGen.Name);

            while (tg is object) { // Add a link slot to each of its parent scopes
                var framegen = tg.FrameGen;
                //newtg.Public.Field(framegen, framegen.Name);
                frame.Public.Field(framegen, framegen.Name);
                tg = tg.ParentScope;
            }

            CodeGen clonecg = frame.Public.Override.Method(FrameBaseGen, "Clone");
            var unused3 = clonecg["clone", frame];
            clonecg.Assign(clonecg["clone"], Exp.New(frame));
            clonecg.Assign(clonecg["clone"].Field(origtg.FrameGen.Name), clonecg.This().Field(origtg.FrameGen.Name));
            clonecg.Assign(clonecg["clone"].Field("Instruction"), clonecg.This().Field("Instruction"));
            clonecg.Assign(clonecg["clone"].Field("Caller"), clonecg.This().Field("Caller"));
            clonecg.Assign(clonecg["clone"].Field("Callee"), clonecg.This().Field("Callee"));
            frame.CloneGen = clonecg;
            //clonecg.Return(clonecg["clone"]);

            {

                MethodGen method_wrapper = null;
                ConstructorGen method_constructor = null;
                if (inputString.Match(subNameMatch) == "new")
                {
                    method_constructor = currentClassGen.Public.Constructor();
                }
                else
                {
                    method_wrapper = currentClassGen.Public.Override.Method(returnType, inputString.Match(subNameMatch));
                }

                var binder_method = newtg.MethodImplementation(intf, FrameBaseGen, "Bind")
                    .Parameter(FrameBaseGen, "caller")
                    .Parameter(currentClassGen, "__csmeta_invocant");

                foreach (var param in paramList) {
                    binder_method.Parameter(param.Type, param.Name);
                    if (inputString.Match(subNameMatch) == "new")
                    {
                        method_constructor.Parameter(param.Type, param.Name);
                    }
                    else
                    {
                        method_wrapper.Parameter(param.Type, param.Name);
                    }
                }

                CodeGen binder = binder_method;
                CodeGen method;
                if (inputString.Match(subNameMatch) == "new")
                {
                    method = method_constructor;
                }
                else
                {
                    method = method_wrapper;
                }

                var unused = binder["frame", frame];
                unused = method["frame", frame];

                binder.Assign(binder["frame"], Exp.New(frame));
                method.Assign(method["frame"], Exp.New(frame));

                if (origtg.Name != "TopLevelFrame") {
                    binder.Assign(binder["frame"].Field("TopLevelFrame"), binder.This().Field(origtg.FrameGen.Name).Field("TopLevelFrame"));
                    method.Assign(method["frame"].Field("TopLevelFrame"), method.This().StaticField("_Closure_" + counter).Field(origtg.FrameGen.Name).Field("TopLevelFrame"));
                } else {
                    binder.Assign(binder["frame"].Field("TopLevelFrame"), binder.This().Field("TopLevelFrame"));
                    method.Assign(method["frame"].Field("TopLevelFrame"), method.This().StaticField("_Closure_" + counter).Field("TopLevelFrame"));
                }


                //binder.WriteLine("assigned TopLevelFrame in " + newtg.Name + " as " + binder["frame"].Field("TopLevelFrame"));

                binder.Assign(binder["frame"].Field("_Closure_" + counter), binder.This());
                method.Assign(method["frame"].Field("_Closure_" + counter), method.This().StaticField("_Closure_" + counter));
                clonecg.Assign(clonecg["clone"].Field("_Closure_" + counter), clonecg.This().Field("_Closure_" + counter));

                tg = origtg;
                var first = true;
                while (tg is object && tg.Name != "TopLevelFrame") { // Add a link to each of its parent scopes
                    if (first || origtg != tg) {
                        first = false;
                        var framegen = tg.FrameGen;
                        //Console.WriteLine("adding link to " + tg.Name + " in binder of " + newtg);
                        //binder.WriteLine("adding link to: " + tg.Name);
                        clonecg.Assign(clonecg["clone"].Field(framegen.Name), clonecg.This().Field(framegen.Name));
                        binder.Assign(binder["frame"].Field(framegen.Name), binder.This().Field(framegen.Name));
                        method.Assign(binder["frame"].Field(framegen.Name), method.This().StaticField("_Closure_" + counter).Field(framegen.Name));
                        //binder.WriteLine("assigned a field from the object: " + binder.This().Field(tg.Name));
                        //binder.WriteLine("assigned it the value: " + binder["frame"].Field(tg.Name));
                    }
                    tg = tg.ParentScope;
                }

                foreach (var param in paramList) {
                    var type = param.Type;
                    var name = param.Name;
                    FrameLocal local;
                    local = new FrameLocal(name, param.Type, newFrameScope);
                    newFrameScope.AddHere(name, local);
                    frame.Public.Field(param.Type, local.MangledName);
                    local.Invokable = true;
                    //newtg.Public.Field(type, local.MangledName);
                    binder.Assign(binder["frame"].Field(local.MangledName), binder[name]);
                    method.Assign(method["frame"].Field(local.MangledName), method[name]);
                }

                binder.Assign(binder["frame"].Field("__csmeta_invocant"), binder.Arg("__csmeta_invocant"));
                binder.Assign(binder["frame"].Field("Caller"), binder.Arg("caller"));
                binder.Return(binder["frame"]);
                binder.Complete();

                method.Assign(method["frame"].Field("__csmeta_invocant"), method.This());
                method.Invoke(method["frame"], "Run");
                if (inputString.Match(subNameMatch) != "new")
                    method.Return(method["frame"].Cast(retIntf).Invoke("GetReturnValue"));
                method.Complete();
            }
            cg4.Assign(cg4.This().Field(origtg.FrameGen.Name), cg4.Arg(origtg.FrameGen.Name));

            var init_method = currentClassGen.Public.Static.Method(typeof(void), "__csmeta_init_" + newtg)
                .Parameter(newtg, "closure");

            CodeGen imcg = init_method;

            imcg.Assign(new TypeLiteral(currentClassGen).StaticField(newtg.ToString()), imcg["closure"]);
            imcg.Complete();

            string subName;
            if (subNameMatch != null && subNameMatch.Success) { // it's a named sub, so declare it as such, in case it's referenced internally.
                subName = inputString.Match(subNameMatch);
            } else {
                subName = "l" + (++FrameScope.Counter).ToString();
            }
            var local2 = new FrameLocal(subName, intf, frameScope);
            local2.Invokable = true;
            frameScope.AddHere(subName, local2);
            //Console.WriteLine("Declared sub " + subName);
            origtg.FrameGen.Public.Field(intf, local2.MangledName);
            newtg.NamedSubMangledName = local2.MangledName;
            ClassesByName.Add(newtg.Name, newtg);
            return newtg;
        }

        public Type ResolveReturnType(Type returnType) {
            return ReturnInterfaces["IReturn_" + returnType.FullName.Replace(".", "Dot").Replace("`", "Backtick")];
        }

        public Operand ResolveClosureType(AssemblyGen ag, Match typeParamsMatch) {
            Type returnType;
            var closureTypeName = (returnType = (typeParamsMatch["returnType"].Result as TypeLiteral).ConstantValue as Type).FullName;
            var paramList = new List<Param>();
            int count = 0;
            while (typeParamsMatch != null && typeParamsMatch.HasOwnKey("typeParamsList") && typeParamsMatch["typeParamsList"].HasOwnKey("nextTypeParam") && (typeParamsMatch["typeParamsList"]["nextTypeParam"].Result as object) != null) {
                var arg = typeParamsMatch["typeParamsList"]["nextTypeParam"];
                var argType = (arg.Result as TypeLiteral).ConstantValue as Type;
                paramList.Add(new Param(argType, "aa" + ++count, false));
                typeParamsMatch = typeParamsMatch["typeParamsList"];
            }
            foreach (var param in paramList)
                closureTypeName += "|" + param.Type.FullName;

            TypeGen intf;
            //Console.WriteLine("found return type for closure interface: " + returnType);
            if (!ClosuresByInterface.TryGetValue(closureTypeName, out intf))
            {
                TypeGen retIntf;
                var returnInterfaceName = "IReturn_" + returnType.FullName.Replace(".", "Dot").Replace("`", "Backtick");
                MethodGen intf_binder;
                int counter = ++FrameCounter;
                ReturnTypesByInterface.Add("_IClosure_" + counter, returnType);
                //Console.WriteLine("Added interface " + "_IClosure_" + counter + " with return type " + returnType);
                intf = ag.Public.Interface("_IClosure_" + counter);
                if (!ReturnInterfaces.TryGetValue(returnInterfaceName, out retIntf)) {
                    retIntf = ag.Public.Interface(returnInterfaceName);
                    MethodGen intf_returner = retIntf.Method(returnType, "GetReturnValue");
                    ReturnInterfaces.Add(returnInterfaceName, retIntf);
                    //retIntf.Complete();
                }
                intf.ReturnIntGen = retIntf;
                //Console.WriteLine("set return interface gen to " + retIntf);
                ClosuresByName.Add("_IClosure_" + counter, intf);
                ClassesByName.Add("_IClosure_" + counter, intf);
                intf_binder = intf.Method(FrameBaseGen, "Bind")
                    .Parameter(FrameBaseGen, "caller");

                foreach (var param in paramList)
                    intf_binder.Parameter(param.Type, param.Name);
                //Console.WriteLine("Adding1 " + closureTypeName);
                ClosuresByInterface.Add(closureTypeName, intf);
            }
            return new TypeLiteral(intf);
        }

        // construct a class for each subroutine to store the locals and a link to the outer frame
        public CodeGen DescendIntoSub(CodeGen cg, TypeGen tg, TypeGen newtg)
        {
            CodeGens.Push(cg);
            CodeGen newcg = newtg.FrameGen.Public.Override.Method(FrameBaseGen, "Exec");
            // add part of the prologue
            var unused = newcg["next_frame", FrameBaseGen];
            newcg.GotoFalse(newcg.This().Field("Instruction").EQ(0), "inst0");
            newcg.Label("inst1");
            newcg.Increment(newcg.This().Field("Instruction"));
            newcg.InstructionCount++;
            return newcg;
        }

        public Label[] GetRoutineInstructionLabels(CodeGen cg)
        {
            var labels = new List<Label>();
            for (var i = 0; i <= cg.InstructionCount; ++i)
            {
                labels.Add(cg.Labels["inst" + i]);
            }
            return labels.ToArray();
        }

        public TypeGen AscendOutofSub(TypeGen tg) {
            tg.FrameGen.CloneGen.Return(tg.FrameGen.CloneGen["clone"]);
            TypeGen tg2 = null;
            try
            {
                tg2 = TypeGens.Pop();
            }
            catch (Exception e)
            {

            }
            return tg2;
        }

        public CodeGen AscendOutofSub(int foo) {
            return CodeGens.Pop();
        }

        public Operand AssignToContextual(CodeGen c, Operand varname, Operand value) {
            // parsetime
            c.If(c["current_scope"].Invoke("ContainsKey", varname).LogicalNot());
            {
                
            }
            c.End();

            var mangled_name = c["current_scope"][varname].Field("MangledName");
            var match = c["m"];
            var tg = c["current_typegen"].Field("FrameGen");

            c.Assign(tg.Field("AssignsToContextual"), true);

            // parsetime
            // if the contextual was declared in this block, just assign to it and be done.
            c.If(c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName").EQ(tg.Invoke("GetName")));
            {
                c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", mangled_name), value);
            }
            c.Else();
            {
                // parsetime
                // if the current block hasn't been given slots to store its reversion data, create them.
                c.If(tg.Field("Contextuals").Invoke("Contains", mangled_name).LogicalNot());
                {
                    c.Invoke(tg.Field("Contextuals"), "Add", mangled_name);
                    //c.WriteLine("_assigned_to_" + mangled_name);
                    //c.WriteLine(tg.Invoke("GetName"));
                    c.Invoke(tg.Property("Public"), "Field", typeof(int), "_assigned_to_" + mangled_name);
                    c.Invoke(tg.Property("Public"), "Field", value.Property("Type"), mangled_name);
                }
                c.End();

                // runtime
                c.Invoke(c["cg"], "If", c["cg"].Invoke("This").Invoke("Field", "_assigned_to_" + mangled_name).Invoke("EQ", 0));
                { // assign to the revert slot iff we haven't already done so in this frame
                    c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", mangled_name), c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", mangled_name));
                    c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", mangled_name), value);
                    c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_assigned_to_" + mangled_name), 1);
                }
                c.Invoke(c["cg"], "Else");
                { // just assign to the contextual again
                    c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", mangled_name), value);

                }
                c.Invoke(c["cg"], "End");

                // parsetime
                c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", mangled_name));
            }
            c.End();
            return c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", mangled_name);
        }

        public void AddExpression(string expressionName, string termName, string termishName) {
            bool hadTop = TOP != null;

            AddPattern(expressionName, All(
                new PatternRef(termName),
                new Repetition(Alt(new PostCircumfixOp(new PatternRef(expressionName)), new PostfixOp()), 0, -1),
                new Repetition(All(new InfixOp(), new PatternRef(expressionName)), 0, -1)
            ));

            AddPattern(termName, Alt(
                All(new PrefixOp(), new PatternRef(expressionName)),
                new CircumfixOp(new PatternRef(expressionName)),
                new PatternRef(termishName)
            ));
            if (!hadTop)
                TOP = null;
        }

        public class ProtoPattern : PrototypeChain<string, Pattern> {
            public ProtoPattern()
                : base() {
            }
            public ProtoPattern(ProtoPattern parent)
                : base(parent) {
            }
        }

        public class ProtoPatternSet : PrototypeChain<string, ProtoPattern> {
            public ProtoPatternSet()
                : base() {
            }
            public ProtoPatternSet(ProtoPatternSet parent)
                : base(parent) {
                foreach (string sym in parent.Keys) {
                    this[sym] = new ProtoPattern(parent[sym]);
                }
            }
        }

        public void AddPattern(string name, Pattern pattern) {
            if (HasOwnKey(name))
                throw new InvalidOperationException("pattern " + name + " already defined in grammar " + Name);
            this.Add(name, pattern = new PatternCall(pattern));
            if (null == TOP)
                TOP = pattern;
        }

        public void AddProto(string name) {
            if (Protos.HasOwnKey(name))
                throw new InvalidOperationException("proto " + name + " already defined in grammar " + Name);
            if (Protos.ContainsKey(name))
                Protos.Add(name, new ProtoPattern(Protos[name]));
            else
                Protos.Add(name, new ProtoPattern());
        }

        public void AddToProto(string name, string symbol, Pattern pattern) {
            if (!Protos.ContainsKey(name))
                throw new InvalidOperationException("proto " + name + " not defined in grammar " + Name);
            if (!Protos.HasOwnKey(name))
                Protos.Add(name, new ProtoPattern(Protos[name]));
            if (Protos[name].HasOwnKey(symbol))
                throw new InvalidOperationException("symbol " + symbol + " in proto " + name + " already defined in grammar " + Name);
            Protos[name].Add(symbol, pattern);
        }

        public static DynamicMethodGen GetRuntimeDMGEntryPoint() {
            return DynamicMethodGen.Static(typeof(Object))
                .Method(typeof(object)).Parameter(typeof(Invoker), "invoker");
        }

        public static DynamicMethodGen GetDMG() {
            return DynamicMethodGen.Static(typeof(Grammar))
                .Method(typeof(Match)) // it returns a Match
                .Parameter(typeof(Matcher), "M") // matcher object (so it can refer to and mutate/recompile itself)
                .Parameter(typeof(UTF32String), "IN") // input
                .Parameter(typeof(int), "o") // initial offset
                .Parameter(typeof(uint), "b") // initial branch (goto label)
                .Parameter(typeof(State), "s"); // initial State object
        }

        public ParserRoutine GetEntryPoint(DynamicMethodGen dmg) {
            var dm = dmg.GetCompletedDynamicMethod(true);
            // separate lines only for easier breakpoint/inspection of the DynamicMethod CIL
            // (I use the DynamicMethod Visualizer, ILVisualizer, in Visual Studio 2008/2010)
            return dm.CreateDelegate(typeof(ParserRoutine)).CastTo<ParserRoutine>();
        }

        public interface IParser {
            Match Parse(Matcher m, UTF32String IN, int o, uint b, State s);
            Grammar Regen();
        }


        public static int assemblyIteration;
        public string GetNextAssemblyName() {
            return "asmbly_" + (++assemblyIteration).ToString();
        }

        public string GetNextEphemeral() {
            return "ephemeral_" + (++EphemeralCounter).ToString();
        }

        public string GetLastEphemeral() {
            return "ephemeral_" + (EphemeralCounter).ToString();
        }

        public void Die(string warning)
        {
            Warn(warning);
            Die();
        }

        public void Die()
        {
            Environment.Exit(1);
        }

        public void Warn(string warning)
        {
            var stderr = Console.OpenStandardError();
            var bytes = (new System.Text.ASCIIEncoding()).GetBytes(warning);
            stderr.Write(bytes, 0, bytes.Length);
            stderr.Close();
        }

        public Match Parse(UTF32String input) {
            //try
            {
                if (MainClass.BuildFirstStage)
                {
                    if (MainClass.SaveStage1AssemblyToDisk)
                    {
                        if (!Compiled)
                        {
                            CompileToDisk();
                            var a = Assembly.LoadFile(GetAssemblyPath());
                            //Console.WriteLine("finished compiling/loading grammar");
                            Matcher = new Matcher(this);
                            Matcher.CompiledParser = (IParser)a.CreateInstance(Name);
                        }
                        Matcher.LastMatch = new Match();
                        return Matcher.CompiledParser.Parse(Matcher, input, 0, 0, new State(State.FirstState, 0));
                    }
                    else
                    {
                        if (!Compiled)
                        {
                            CompileToMemory();
                            //Console.WriteLine("finished compiling/loading grammar");
                        }
                        Matcher.LastMatch = new Match();
                        return Matcher.Wrapped(Matcher, input, 0, 0, new State(State.FirstState, 0));
                    }
                    //return true;
                }
                else
                {
                    Matcher = new Matcher(this);
                    //Matcher.LastMatch = new Match();
                    return ((IParser)(new hi())).Parse(Matcher, input, 0, 0, new State(State.FirstState, 0));
                }
            }
            //catch (Exception e)
            {
            //    Die(e.ToString() + "\n");
            }
            return null;
        }

        public string GetAssemblyPath() {
            return GetAssemblyPath(Name + ".exe");
        }

        public string GetAssemblyPath(string name) {
            return Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + name;
        }

        public void CompileToMemory() {
            DynamicMethodGen dmg = GetDMG();
            CodeGen = dmg;
            var m = Compile(dmg);
            m.Wrapped = GetEntryPoint(dmg);
            Matcher = m;
        }

        public TypeGen FrameBaseGen;

        public AssemblyGen GetAssemblyGen(string name) {
            var ag = new AssemblyGen(GetAssemblyPath(name + ".exe"));
            var frame = ag.Public.Abstract.Class("FrameBase");
            ClassesByName.Add("FrameBase", frame);
            frame.Public.Field(typeof(int), "Instruction");
            frame.Public.Field(frame, "Caller");
            frame.Public.Field(frame, "Callee");
            var exec = frame.Public.Abstract.Method(frame, "Exec");
            var clone = frame.Public.Abstract.Method(frame, "Clone");
            var run = frame.Public.Method(typeof(void), "Run");
            CodeGen cg = run;
            var unused = cg["next", frame];
            cg.Assign(cg["next"], cg.This());
            cg.Label("do_next");
            //cg.WriteLine(cg["next"] + " " + cg["next"].Field("Instruction"));
            cg.Assign(cg["next"], cg["next"].InvokeOverride("Exec"));
            cg.GotoTrue(cg["next"].IsNotNull(), "do_next");
            FrameBaseGen = frame;
            //run = frame.Public.Method(typeof(void), "Say");
            //cg = run;
            //cg.WriteLine("gh1 " + cg.This());
            return ag;
        }

        public void InvokeEntryPoint(Assembly assembly) {
            var t = assembly.GetType("MainClass");
            var i = assembly.CreateInstance("MainClass");
            t.GetMethod("Main").Invoke(i, new object[] { new string[] { } });
        }

        public void SaveAndInvokeAssembly(AssemblyGen ag, string assemblyName) {
            ag.Save();
            Object[] args = new Object[] { new String[0] };
            Assembly.LoadFile(GetAssemblyPath(assemblyName + ".exe")).EntryPoint.Invoke(null, args);
        }

        public TypeGen GetTypeGen(AssemblyGen ag) {
            return ag.Public.Class("MainClass");
        }

        public TypeGen GetToplevelTypeGen(AssemblyGen ag) {
            var tlf = ag.Public.Sealed.Class("TopLevelFrame", FrameBaseGen);
            tlf.FrameGen = tlf; // it's its own framegen.  :/
            CodeGen clonecg = tlf.Public.Override.Method(FrameBaseGen, "Clone");
            var unused3 = clonecg["clone", tlf];
            clonecg.Assign(clonecg["clone"], Exp.New(tlf));
            //clonecg.Assign(clonecg["clone"].Field("TopLevelFrame"), clonecg.This().Field("TopLevelFrame"));
            clonecg.Assign(clonecg["clone"].Field("Instruction"), clonecg.This().Field("Instruction"));
            clonecg.Assign(clonecg["clone"].Field("Caller"), clonecg.This().Field("Caller"));
            clonecg.Assign(clonecg["clone"].Field("Callee"), clonecg.This().Field("Callee"));
            tlf.CloneGen = clonecg;
            //clonecg.Return(clonecg["clone"]);
            /*
            {
                var ret_getter = tlf.Public.Property(typeof(object), "ReturnValue");
                MethodGen ggg = ret_getter.Getter();
                CodeGen g = ggg;
                g.Return(g.This().Field("Return"));
                tlf.setImplementation2(ggg);
            }
            */
            //tlf.Field(tlf, "TopLevelFrame");
            return tlf;
        }

        public MethodGen GetEntryPointGen(TypeGen gc) {
            if (MainClass.SaveStage2AssemblyToDisk) {
                return gc.Static.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
            } else {
                return gc.Public.Method(typeof(void), "Main").Parameter(typeof(string[]), "args");
            }
        }

        public MethodGen GetExecGen(TypeGen gc)
        {
            var toplevel_exec = gc.Public.Override.Method(FrameBaseGen, "Exec");
            CodeGen newcg = toplevel_exec;
            newcg.GotoFalse(newcg.This().Field("Instruction").EQ(0), "inst0");
            newcg.Label("inst1");
            newcg.Increment(newcg.This().Field("Instruction"));
            newcg.InstructionCount++;
            newcg.Increment(newcg.This().Field("Instruction"));
            newcg.InstructionCount++;
            return toplevel_exec;
        }

        public MethodGen GetBindGen(TypeGen gc, Type parentType) {
            return gc.Public.Override.Method(typeof(void), "BindToParent").Parameter(parentType, "parentFrame");
        }

        public void RunAssembly(string name) {
            Object[] args = new Object[] { new String[0] };
            Assembly.LoadFile(GetAssemblyPath(name + ".exe")).EntryPoint.Invoke(null, args);
        }

        public Operand GetOperand(string str) {
            return Operand.FromObject(str);
        }

        public void CompileToDisk() {
            AssemblyGen ag = new AssemblyGen(GetAssemblyPath());
            var gc = ag.Public.Class(Name, typeof(Object), typeof(IParser));
            //gc.Public.Constructor();
            var dmg = gc.MethodImplementation(typeof(IParser), typeof(Match), "Parse")
                .Parameter(typeof(Matcher), "M") // matcher object (so it can refer to and mutate/recompile itself)
                .Parameter(typeof(UTF32String), "IN") // input
                .Parameter(typeof(int), "o") // initial offset
                .Parameter(typeof(uint), "b") // initial branch (goto label)
                .Parameter(typeof(State), "s"); // initial State object
            CodeGen = dmg;
            Compile(dmg);
            dmg = gc.MethodImplementation(typeof(IParser), typeof(Grammar), "Regen");
            var cg = (CodeGen)dmg;
            var unused = cg["newg", typeof(Grammar)];
            cg.InvokeAssign(cg["newg"], typeof(Sprixel.Perlesque), "BuildPerlesque", "perlesque_prime", false, false);
            cg.Return(cg["newg"]);
            var mc = ag.Public.Class("MainClass", typeof(object));
            var mmg = mc.Static.Method(typeof(void), "Main")
                .Parameter(typeof(string[]), "args");
            cg = mmg;
            unused = cg["g", typeof(Grammar)];
            unused = cg["p", gc];
            cg.Assign(cg["p"], Exp.New(gc));
            cg.InvokeAssign(cg["g"], cg["p"], "Regen");
            cg.If(cg.Arg("args").Property("Length") > 0 && cg.Arg("args")[0].Property("Length") > 0);
            {
                unused = cg["input", typeof(string)];
                cg.InvokeAssign(cg["input"], typeof(System.IO.File), "ReadAllText", cg.Arg("args")[0]);
                //cg.Assign(cg["g"].Field("Matcher"), Exp.New(typeof(Matcher), cg["g"]));
                unused = cg["s", typeof(State)];
                unused = cg["u", typeof(uint)];
                cg.Assign(cg["u"], 0);
                cg.InvokeAssign(cg["s"], typeof(State), "GetFirstState");
                cg.Invoke(cg["p"], "Parse", Exp.New(typeof(Matcher), cg["g"]), Exp.New(typeof(UTF32String), Sprixel.MainClass.settingString + cg["input"]), 0, cg["u"], Exp.New(typeof(State), cg["s"], 0));
            }
            cg.End();

            ag.Save();
        }

        public void Compile() {
            if (!Compiled) {
                CompileToDisk();
            }
        }

        public Matcher Compile(CodeGen cg) {

            // preprocess the protos
            foreach (var name in Protos.Keys) {
                var proto = Protos[name];
                var alts = new List<Pattern>();
                foreach (var sym in proto.Keys) {
                    var pat = proto[sym];
                    if (pat is ProtoSymbol)
                        alts.Add(new Literal(sym));
                    else {
                        pat.ResolveSym(sym, null, false);
                        alts.Add(pat);
                    }
                }
                if (alts.Count > 0)
                    this[name] = new PatternCall(Grammar.Alt(alts.ToArray()));
            }

            // recursively compute full set of named references for each 
            //   pattern, preventing descent into a cyclical reference.
            var keys = this.Keys.ToArray();
            foreach (var name in keys) {
                var pat = this[name];
                var refs = new HashSet<string>();
                var isRecursive = false;
                pat.ComputeRefs(this, name, refs);
                foreach (var refName in refs) {
                    if (refName == name)
                        isRecursive = true;
                    if (!ContainsKey(refName))
                        throw new InvalidOperationException("Pattern " + name + " in grammar " + Name + " contains unresolved reference to " + refName);
                }
                pat.NamedRefs = refs;

                if (pat.Recursive = isRecursive) {
                    // Console.WriteLine(("recursive pattern: " + name));
                    this[name].Recursive = true;
                }
            }

            var m = new Matcher(this);
            var p = (TOP as PatternCall).L.Regen(this);
            var unused = CodeGen["l", typeof(int)];
            // int local "l" stores length of the input, in Unicode (21-bit, stored as 32-bit) characters
            CodeGen.Assign(CodeGen["l"], CodeGen["IN"].Field("Length"));
            // CodeGen["o"] is current offset (position in the input)
            // CodeGen["s"] is current state object
            unused = CodeGen["i", typeof(UInt32[])]; // input array alias
            unused = CodeGen["c", typeof(Int32[])]; // target buf32 (int[]) to match in literal matcher
            unused = CodeGen["i1", typeof(UInt32)]; // temporary uint
            unused = CodeGen["i2", typeof(int)]; // temporary int
            unused = CodeGen["i3", typeof(int)]; // temporary int
            unused = CodeGen["str_3", typeof(string)]; // temporary str
            unused = CodeGen["v", typeof(UInt32)]; // temporary uint
            CodeGen.Assign(CodeGen["v"], 0);
            unused = CodeGen["m", typeof(Match)]; // current match object
            unused = CodeGen["s2", typeof(State)]; // transitory state object used by Repetition
            unused = CodeGen["call", typeof(RefCall)]; // current return frame "pointer" (goto label)
            unused = CodeGen["operand01", typeof(Operand)]; // temporary operand var
            unused = CodeGen["operand02", typeof(Operand)]; // temporary operand var
            unused = CodeGen["type1", typeof(Type)]; // temporary type var
            CodeGen.Assign(CodeGen["call"], null);
            unused = CodeGen["GG", typeof(Grammar)]; // current grammar
            unused = CodeGen["cgc", typeof(CircumfixGoalContainer)]; // circumfix goal container
            unused = CodeGen["pp", typeof(string)]; // precedence parameter
            CodeGen.Assign(CodeGen["cgc"], Exp.New(typeof(CircumfixGoalContainer))); // circumfix goal container
            CodeGen.Assign(CodeGen["pp"], "_"); // current precedence minimum (for the precedence climbing OPP)
            CodeGen.Assign(CodeGen["m"], CodeGen["M"].Field("LastMatch")); // current match
            CodeGen.Assign(CodeGen["GG"], CodeGen["M"].Field("Grammar")); // current grammar
            // unsigned int[] local "i" is the array of the input characters.
            // TODO: in the stream-processing edition, it's an indexer (get_Item(int))
            CodeGen.Assign(CodeGen["i"], CodeGen["IN"].Field("Chars"));
            var done = Transition.Next;
            var success = Transition.Next;
            var failer = Transition.Next;
            var matchfail = Transition.Next;
            DoneLabel = success;

            p.Emit(this);

            CodeGen.Goto(success);
            CodeGen.Label(ParseFailLabel = p.Fail);
            CodeGen.Goto(matchfail);

            var keys2 = this.Keys.ToArray();
            foreach (var name in keys2) {
                var pat = this[name];
                if (pat != TOP)
                    pat.Emit(this);
            }
            CodeGen.WriteLine("There was a compiler failure; please contact the author. Fell off the end of the non-TOP patterns.");

            CodeGen.Label(failer);
            CodeGen.WriteLine("There was a compiler failure; please contact the author.");
            CodeGen.WriteLine("Tried to goto label " + CodeGen["i1"]);
            CodeGen.Goto(p.Fail);

            if (null != p as IBackTracking) {
                CodeGen.Label((p as IBackTracking).Done);
                CodeGen.Label((p as IBackTracking).Notd);
            }
            CodeGen.Goto(success);

            CodeGen.Label(SwitchLabel);
            //CodeGen.WriteLine("switching to label " + CodeGen["i1"]);
            CodeGen.Assign(CodeGen["call"], CodeGen["call"].Field("Last"));

            var labels = CodeGen.MarkedLabels.Keys;
            int maxLabelName = 0;
            int labelInt;

            foreach (var s in labels)
                if (Int32.TryParse(s, out labelInt) && labelInt > maxLabelName)
                    maxLabelName = labelInt;

            // order the labels by their original labelname integer
            var orderedLabels = new Label[maxLabelName + 1];
            Label label;
            for (var i = 0; i <= maxLabelName; ++i) {
                orderedLabels[i] = (CodeGen.MarkedLabels.TryGetValue(i.ToString(), out label))
                    ? label
                    : CodeGen.MarkedLabels[failer];
            }
            CodeGen.Switch(CodeGen["i1"], orderedLabels);
            CodeGen.Goto(failer);

            CodeGen.Label(success);
            CodeGen.Assign(CodeGen["m"].Field("End"), CodeGen["o"]);
            CodeGen.Assign(CodeGen["m"].Field("Success"), true);
            CodeGen.Goto(done);

            CodeGen.Label(matchfail);

            CodeGen.Label(done);
            CodeGen.Return(CodeGen["m"]);

            Compiled = true;
            return m;
        }

        public static Pattern Alt(params Pattern[] pats) {
            return pats.Length == 1
                ? pats[0]
                : Either.Discern(pats[0],
                  pats.Length > 2
                    ? Alt(pats.ShiftLeft(1))
                    : pats[1]
                  );
        }

        public static Pattern All(params Pattern[] pats) {
            return pats.Length == 1
                ? pats[0]
                : Both.Discern(pats[0],
                  pats.Length > 2
                    ? All(pats.ShiftLeft(1))
                    : pats[1]
                  );
        }

        public static Dictionary<string, Type> CreatedTypes = new Dictionary<string, Type>();

        public Operand ResolveParametricType(AssemblyGen ag, string typeName, Match typeParamsMatch) {
            Type type;
            var omatch = typeParamsMatch;
            var typeParameters = new List<Operand>();
            while (typeParamsMatch != null && typeParamsMatch.HasOwnKey("typeParamsList") && typeParamsMatch["typeParamsList"].HasOwnKey("nextTypeParam")) {
                var arg = typeParamsMatch["typeParamsList"]["nextTypeParam"];
                if (null != (object)arg.Result) {
                    typeParameters.Add(arg.Result);
                    //Console.WriteLine("arg " + (args.Count - 1) + " is a " + arg.Result.Type);
                } else
                    break;
                typeParamsMatch = typeParamsMatch["typeParamsList"];
            }
            List<Type> typeList = new List<Type>(typeParameters.Count);
            foreach (var param in typeParameters) {
                typeList.Add((param as TypeLiteral).ConstantValue as Type);
            }
            type = ResolveType(typeName + (typeParameters.Count > 0 ? ("`" + typeParameters.Count.ToString()) : ""));
            var tt = type;
            if (typeParameters.Count > 0) {
                var ta = typeList.ToArray();
                try {
                    TypeGen tg1 = new GenTypeGen(type.MakeGenericType(ta), tt, ta);
                    if (!GenericTypeDefinitionsByGenericTypeNames.ContainsKey(tt.FullName))
                        GenericTypeDefinitionsByGenericTypeNames.Add(tt.FullName, tt);
                    type = tg1.type;
                    //(type as TypeBuilder).CreateType();
                    if (CreatedTypes.TryGetValue(type.FullName, out tt))
                        return new TypeLiteral(tt);
                    CreatedTypes.Add(type.FullName, tg1);
                } catch (Exception) {
                    return new TypeLiteral(tt.MakeGenericType(typeList.ToArray()));
                }
            }
            //Console.WriteLine("returning typeliteral of type " + type);
            return new TypeLiteral(type);
        }

        public Type ResolveType(string shortname) {
           
            switch (shortname) {
                case "int": return typeof(Int32);
                case "str": return typeof(string);
                //case "sub": return typeof(sub);
                case "bool": return typeof(bool);
                case "bit": return typeof(int);
                case "buf": return typeof(buf8);
                default:
                    Type t;
                    var longname = shortname.Replace("::", ".");
                    foreach (var prefix in new string[] {
                        "",
                        "Sprixel.Runtime.",
                        "System.",
                        "Sprixel.",
                        "TriAxis.RunSharp.",
                        "TriAxis.RunSharp.Operands",
                        "System.Collections.Generic."
                    }) {
                        if ((t = Type.GetType(prefix + longname, false, true)) != null) {
                            return t;
                        }
                        try {
                            t = Type.GetType(prefix + longname + ", " + SystemAssemblyName, false, true);
                        } catch (Exception e) {
                        }
                        if (t != null)
                            return t;

                        TypeGen tg;
                        ClassesByName.TryGetValue(prefix + longname, out tg);
                        if (tg is object) {
                            t = tg;
                            return t;
                        }
                    }
                    //throw new MissingTypeException(shortname);
                    return null;
            }
        }

        public void AddInfixOperator(string oper, string op) {
            InfixOperators[oper, true, true] = new OperatorDetails("a", AssociativityType.Right, (Grammar gr, string optext, Operand left, Operand right) => {
                var c = gr.CodeGen;
                var match = c["m"];
                c.InvokeAssign(match.Field("Result"), left.Field("Result"), op, right.Field("Result"));
            });
        }

        public void AddInfixMutator(string oper, string op) {
            InfixOperators[oper, true, true] = new OperatorDetails("a", AssociativityType.Right, (Grammar gr, string optext, Operand left, Operand right) => {
                var c = gr.CodeGen;
                var match = c["m"];
                c.AssignAdd(c["VC"], 1); // create a local to store the result
                c.InvokeAssign(c["cg"][c["VC"].Invoke("ToString")], left.Field("Result"), op, right.Field("Result"));
                c.Assign(match.Field("Result"), c["cg"][c["VC"].Invoke("ToString")]);
            });
        }
    }

    public class MissingTypeException : Exception {
        public string typeName;
        public string ToString() {
            return "Can't resolve type "+typeName+".";
        }
        public MissingTypeException(string typeName) {
            this.typeName = typeName;
        }
    }

    public static partial class Extensions {
        public static T[] ShiftLeft<T>(this T[] array, int count) {
            T[] result = new T[array.Length - count];
            System.Array.Copy(array, count, result, 0, result.Length);
            return result;
        }

        public static string[] Split(this string str, string splitter) {
            return str.Split(new[] { str }, StringSplitOptions.None);
        }

        public static bool IsDerivedFrom(this Type child, Type parent) {
            var oc = child;
            do {
                if (child.Equals(parent))
                    return true;
                if (child.Equals(typeof(object)))
                    break;
                child = child.BaseType;
            } while (child is object);
            child = oc;
            if (parent.IsInterface)
                foreach (var child_interface in child.GetInterfaces())
                    if (child_interface.IsDerivedFrom(parent))
                        return true;
            return false;
        }
    }
}
