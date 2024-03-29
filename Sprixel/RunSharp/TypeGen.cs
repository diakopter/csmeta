/*
 * Copyright (c) 2009, Stefan Simek
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */

// with many changes/additions, Copyright (c) 2010, Matthew Wilson
//   made available under the same license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Sprixel;

namespace TriAxis.RunSharp
{
    public interface IDelayedDefinition
    {
        void EndDefinition();
    }

    public interface IDelayedCompletion
    {
        void Complete();
    }

    public class TypeGen : ITypeInfoProvider
    {
        public class InterfaceImplEntry
        {
            IMemberInfo interfaceMethod;
            MethodGen implementation;

            public InterfaceImplEntry(IMemberInfo interfaceMethod)
            {
                this.interfaceMethod = interfaceMethod;
            }

            public bool Match(MethodGen candidate)
            {
                return candidate.Name == interfaceMethod.Name &&
                        candidate.ReturnType == interfaceMethod.ReturnType &&
                        ArrayUtils.Equals(candidate.ParameterTypes, interfaceMethod.ParameterTypes);
            }

            public IMemberInfo InterfaceMethod { get { return interfaceMethod; } }
            public Type InterfaceType { get { return interfaceMethod.Member.DeclaringType; } }
            public MethodGen BoundMethod { get { return implementation; } }

            public bool IsBound { get { return implementation != null; } }

            public void Bind(MethodGen implementation)
            {
                this.implementation = implementation;
            }
        }

        public AssemblyGen owner;
        public string name;
        public Type baseType;
        public Type[] interfaces;
        public TypeBuilder tb;
        public string NamedSubMangledName;

        public TypeGen ParentScope;
        public TypeGen FrameGen;
        public CodeGen CloneGen;
        public TypeGen IntGen;
        public TypeGen ReturnIntGen;
        public bool AssignsToContextual;
        public delegate void PrologueEvent(CodeGen cg);
        public List<PrologueEvent> PrologueEvents = new List<PrologueEvent>();

        public Dictionary<string, string> MangledContextuals = new Dictionary<string, string>();

        public void RevertContextualsIn(FrameScope fs, CodeGen cg) {
            foreach (var varname in MangledContextuals.Values) {
                if (fs.AssignedContextuals.Contains(varname)) {
                    if (cg.This().Type.Name != "TopLevelFrame") {
                        cg.If(cg.This().Field("_assigned_" + fs.ID + "_" + varname) > 0);
                        {
                            cg.Assign(cg.This().Field("TopLevelFrame").Field("_ctxl_" + varname), cg.This().Field("TopLevelFrame").Field("_stack_" + varname).Invoke("Pop"));
                            cg.Assign(cg.This().Field("_assigned_" + fs.ID + "_" + varname), 0);
                        }
                        cg.End();
                    } else {
                        cg.If(cg.This().Field("_assigned_" + fs.ID + "_" + varname) > 0);
                        {
                            cg.Assign(cg.This().Field("_ctxl_" + varname), cg.This().Field("_stack_" + varname).Invoke("Pop"));
                            cg.Assign(cg.This().Field("_assigned_" + fs.ID + "_" + varname), 0);
                        }
                        cg.End();
                    }
                }
            }
        }
        
        // generates code that resets the lexical slots in the enclosing frame for re-entering
        //   a block (such as in a loop).  Effectively emulates a parameterless closure.
        public void RevertLexicalsIn(FrameScope fs, CodeGen cg) {
            foreach (var varname in fs.OwnKeysList) {
                if (fs[varname].Type.IsValueType) {
                    cg.This().Field(fs[varname].MangledName).ForceEmitAddress(cg);
                    cg.IL.Emit(OpCodes.Initobj, fs[varname].Type);
                } else {
                    cg.Assign(cg.This().Field(fs[varname].MangledName), null);
                }
            }
        }
        
        // this routine runs at parser-runtime (not parser-compiletime nor program-runtime)
        public string GetMangledContextual(TypeGen currentFrameBuilder, FrameScope currentScope, FrameScope topLevelScope, string varname) {
            string mangledName;
            if (!MangledContextuals.TryGetValue(varname, out mangledName)) {
                throw new InvalidOperationException("you must declare the contextual " + varname + ", including its type annotation");
            }
            return GetMangledContextual(currentFrameBuilder, currentScope, topLevelScope, topLevelScope[varname].Type, varname);
        }

        // this routine runs at parser-runtime (not parser-compiletime nor program-runtime)
        public string GetMangledContextual(TypeGen currentFrameBuilder, FrameScope currentScope, FrameScope topLevelScope, Type type, string varname) {
            string mangledName;
            if (!MangledContextuals.TryGetValue(varname, out mangledName)) {
                var frameLocal = new FrameLocal(varname, type, topLevelScope);
                topLevelScope.AddHere(varname, frameLocal);
                currentFrameBuilder.FrameGen.MangledContextuals.Add(varname, mangledName = frameLocal.MangledName);
                // create a field in the toplevel frame class for the current contextual value
                Public.Field(type, "_ctxl_" + mangledName);
                // and create a runtime type representing the type of the Stack<TypeOfThisContextual>
                var stack_type = typeof(Stack<>).MakeGenericType(type);
                // create a field in the toplevel frame class for the stack of previously assigned dynamic
                //   values of the contextual variable.
                Public.Field(stack_type, "_stack_" + mangledName);
                // push a closure that will run (parser-runtime) at the end of the toplevel routine declaration
                //   (but at program-runtime will run as a prologue to the program) that initializes this stack
                PrologueEvents.Add((CodeGen cg) => {
                    if (cg.This().Type.Name == "TopLevelFrame") {
                        cg.Assign(cg.This().Field("_stack_" + mangledName), Exp.New(stack_type));
                    } else {
                        cg.Assign(cg.This().Field("TopLevelFrame").Field("_stack_" + mangledName), Exp.New(stack_type));
                    }
                });
            }
            // if we haven't already added a flag assigned slot for this contextual-scope tuple in this frame,
            if (!currentScope.AssignedContextuals.Contains(mangledName)) {
                // record that we've done so,
                currentScope.AssignedContextuals.Add(mangledName);
                // and create a field in the current frame class for a flag representing whether to pop the 
                //   contextual stack upon leaving this scope,
                currentFrameBuilder.FrameGen.Public.Field(typeof(int), "_assigned_" + currentScope.ID + "_" + mangledName);
            }
            return mangledName;
        }

        public void EmitPrologueInitializers(CodeGen cg) {
            foreach (var pe in PrologueEvents) {
                pe.Invoke(cg);
            }
        }

        public Type _ClosureReturnType;
        public Type CRT // ClosureReturnType
        {
            get
            {
                return _ClosureReturnType;
            }
            set
            {
                if (_ClosureReturnType != null)
                {
                    if (!_ClosureReturnType.Equals(value))
                    {
                        throw new InvalidOperationException("this routine already returns something of type " + _ClosureReturnType.Name);
                    }
                }
                else
                {
                    _ClosureReturnType = value;
                }
            }
        }

        public Type GetTypeObject()
        {
            return (Type)tb;
        }

        public string GetName()
        {
            return name;
        }

        public TypeBuilder GetTypeBuilderObject()
        {
            return tb;
        }

        public Sprixel.FrameScope FrameScope;

        public Type type;
        public MethodGen commonCtor = null;
        public ConstructorGen staticCtor = null;
        public List<IDelayedDefinition> definitionQueue = new List<IDelayedDefinition>();
        public List<IDelayedCompletion> completionQueue = new List<IDelayedCompletion>();
        public List<TypeGen> nestedTypes = new List<TypeGen>();
        public List<InterfaceImplEntry> implementations = new List<InterfaceImplEntry>();

        public void setImplementation2(MethodGen mg)
        {
            implementations.ToArray()[1].Bind(mg);
        }

        public List<IMemberInfo> constructors = new List<IMemberInfo>();
        public List<IMemberInfo> fields = new List<IMemberInfo>();
        public List<IMemberInfo> properties = new List<IMemberInfo>();
        public List<IMemberInfo> events = new List<IMemberInfo>();
        public List<IMemberInfo> methods = new List<IMemberInfo>();
        public List<AttributeGen> customAttributes = new List<AttributeGen>();
        public string indexerName;

        public TypeBuilder TypeBuilder { get { return tb; } }
        public Type BaseType { get { return baseType; } }

        public string Name { get { return name; } }

        public TypeGen() { }

        public TypeGen(AssemblyGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces)
        {
            this.owner = owner;
            this.name = name;
            this.baseType = baseType;
            this.interfaces = interfaces;

            tb = owner.ModuleBuilder.DefineType(name, attrs, baseType, interfaces);
            owner.AddType(this);
            ScanMethodsToImplement(interfaces);

            TypeInfo.RegisterProvider(tb, this);
            ResetAttrs();
        }

        public TypeGen(TypeGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces)
        {
            this.owner = owner.owner;
            this.name = name;
            this.baseType = baseType;
            this.interfaces = interfaces;

            tb = owner.TypeBuilder.DefineNestedType(name, attrs, baseType, interfaces);
            owner.nestedTypes.Add(this);
            ScanMethodsToImplement(interfaces);

            TypeInfo.RegisterProvider(tb, this);
        }

        public void ScanMethodsToImplement(Type[] interfaces)
        {
            if (interfaces == null)
                return;

            foreach (Type t in interfaces)
            {
                foreach (IMemberInfo mi in TypeInfo.GetMethods(t))
                    implementations.Add(new InterfaceImplEntry(mi));
            }
        }

        public MethodAttributes PreprocessAttributes(MethodGen mg, MethodAttributes attrs)
        {
            bool requireVirtual = false;

            foreach (InterfaceImplEntry implEntry in implementations)
            {
                if (!implEntry.IsBound && implEntry.Match(mg))
                {
                    implEntry.Bind(mg);
                    requireVirtual = true;
                }
            }

            if (requireVirtual && ((attrs & MethodAttributes.Virtual) == 0))
                // create an exclusive VTable entry for the method
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;

            return attrs;
        }

        public void RegisterForCompletion(ICodeGenContext routine)
        {
            definitionQueue.Add(routine);
            completionQueue.Add(routine);
        }

        public void RegisterForCompletion(IDelayedCompletion completion)
        {
            completionQueue.Add(completion);
        }

        #region Modifiers
        MethodAttributes mthVis, mthFlags, mthVirt;
        FieldAttributes fldVis, fldFlags;
        TypeAttributes typeVis, typeFlags, typeVirt;
        MethodImplAttributes implFlags;

        public void SetVisibility(MethodAttributes mthVis, FieldAttributes fldVis, TypeAttributes typeVis)
        {
            if (this.mthVis != 0)
                throw new InvalidOperationException("Member visibility can be set only once");

            this.mthVis = mthVis;
            this.fldVis = fldVis;
            this.typeVis = typeVis;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Public { get { SetVisibility(MethodAttributes.Public, FieldAttributes.Public, TypeAttributes.NestedPublic); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Private { get { SetVisibility(MethodAttributes.Private, FieldAttributes.Private, TypeAttributes.NestedPrivate); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Protected { get { SetVisibility(MethodAttributes.Family, FieldAttributes.Family, TypeAttributes.NestedFamily); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Internal { get { SetVisibility(MethodAttributes.Assembly, FieldAttributes.Assembly, TypeAttributes.NestedAssembly); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen ProtectedOrInternal { get { SetVisibility(MethodAttributes.FamORAssem, FieldAttributes.FamORAssem, TypeAttributes.NestedFamORAssem); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen ProtectedAndInternal { get { SetVisibility(MethodAttributes.FamANDAssem, FieldAttributes.FamANDAssem, TypeAttributes.NestedFamANDAssem); return this; } }

        public void SetVirtual(MethodAttributes mthVirt, TypeAttributes typeVirt)
        {
            if (this.mthVirt != 0)
                throw new InvalidOperationException("Member virtual type can be set only once");

            this.mthVirt = mthVirt;
            this.typeVirt = typeVirt;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Sealed { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.Final, TypeAttributes.Sealed); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Virtual { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.NewSlot, 0); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Override { get { SetVirtual(MethodAttributes.Virtual, 0); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Abstract { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.Abstract, TypeAttributes.Abstract); return this; } }

        public void SetFlag(MethodAttributes mthFlag, FieldAttributes fldFlag, TypeAttributes typeFlag)
        {
            if ((this.mthFlags & mthFlag) != 0 ||
                (this.fldFlags & fldFlag) != 0 ||
                (this.typeFlags & typeFlag) != 0)
                throw new InvalidOperationException(string.Format(null, "Attribute '{0}' has already been set", mthFlag));

            this.mthFlags |= mthFlag;
            this.fldFlags |= fldFlag;
            this.typeFlags |= typeFlag;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen Static { get { SetFlag(MethodAttributes.Static, FieldAttributes.Static, TypeAttributes.Sealed | TypeAttributes.Abstract); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen ReadOnly { get { SetFlag(0, FieldAttributes.InitOnly, 0); return this; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen NoBeforeFieldInit { get { SetFlag(0, 0, TypeAttributes.BeforeFieldInit); return this; } }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TypeGen RuntimeImpl { get { implFlags |= MethodImplAttributes.Runtime | MethodImplAttributes.Managed; return this; } }

        public void ResetAttrs()
        {
            if (tb.IsInterface)
            {
                mthVis = MethodAttributes.Public;
                mthVirt = MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract;
                mthFlags = 0;
            }
            else
                mthVis = mthVirt = mthFlags = 0;

            fldVis = fldFlags = 0;
            typeVis = typeVirt = typeFlags = 0;
            implFlags = 0;
        }
        #endregion

        #region Custom Attributes

        public TypeGen Attribute(AttributeType type)
        {
            BeginAttribute(type);
            return this;
        }

        public TypeGen Attribute(AttributeType type, params object[] args)
        {
            BeginAttribute(type, args);
            return this;
        }

        public AttributeGen<TypeGen> BeginAttribute(AttributeType type)
        {
            return BeginAttribute(type, EmptyArray<object>.Instance);
        }

        public AttributeGen<TypeGen> BeginAttribute(AttributeType type, params object[] args)
        {
            AttributeTargets target = AttributeTargets.Class;

            if (baseType == null)
                target = AttributeTargets.Interface;
            else if (baseType == typeof(ValueType))
                target = AttributeTargets.Struct;
            else
                target = AttributeTargets.Class;

            return AttributeGen<TypeGen>.CreateAndAdd(this, ref customAttributes, target, type, args);
        }

        #endregion

        #region Members
        public MethodGen CommonConstructor()
        {
            if (tb.IsValueType)
                throw new InvalidOperationException("Value types cannot have default constructors");
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define a constructor");

            if (commonCtor == null)
            {
                commonCtor = new MethodGen(this, "$$ctor", 0, typeof(void), 0).LockSignature();
            }

            return commonCtor;
        }

        public ConstructorGen Constructor()
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define a constructor");

            ConstructorGen cg = new ConstructorGen(this, mthVis, implFlags);
            ResetAttrs();
            return cg;
        }

        public ConstructorGen StaticConstructor()
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define a constructor");

            if (staticCtor == null)
            {
                staticCtor = new ConstructorGen(this, MethodAttributes.Static, 0).LockSignature();
            }

            return staticCtor;
        }

        public FieldGen PublicField(Type type, string name)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot have fields");


            if (fldVis == 0)
                fldVis |= FieldAttributes.Public;

            FieldGen fld = new FieldGen(this, name, type, fldVis | fldFlags);
            fields.Add(fld);
            ResetAttrs();
            return fld;
        }

        public FieldGen Field(Type type, string name)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot have fields");

            if (fldVis == 0)
                fldVis |= FieldAttributes.Private;

            FieldGen fld = new FieldGen(this, name, type, fldVis | fldFlags);
            fields.Add(fld);
            ResetAttrs();
            return fld;
        }

        public FieldGen Field(Type type, string name, Operand initialValue)
        {
            FieldGen fld = Field(type, name);

            CodeGen initCode = fld.IsStatic ? StaticConstructor().GetCode() : CommonConstructor().GetCode();
            initCode.Assign(fld, initialValue);
            return fld;
        }

        public PropertyGen Property(Type type, string name)
        {
            if (mthVis == 0)
                mthVis |= MethodAttributes.Private;

            if (tb.IsInterface)
                mthVirt |= MethodAttributes.Virtual | MethodAttributes.Abstract;

            PropertyGen pg = new PropertyGen(this, mthVis | mthVirt | mthFlags, type, name);
            properties.Add(pg);
            ResetAttrs();

            return pg;
        }

        public PropertyGen Indexer(Type type)
        {
            return Indexer(type, "Item");
        }

        public PropertyGen Indexer(Type type, string name)
        {
            if (indexerName != null && indexerName != name)
                throw new InvalidOperationException("Indexers for the type cannot have different names");

            PropertyGen pg = Property(type, name);
            indexerName = name;
            return pg;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "It is invalid to use anything else than a Field as a base for SimpleProperty")]
        public PropertyGen SimpleProperty(FieldGen field, string name)
        {
            if ((object)field == null)
                throw new ArgumentNullException("field");

            PropertyGen pg = Property(field.Type, name);
            pg.Getter().GetCode().Return(field);
            pg.Setter().GetCode().Assign(field, pg.Setter().GetCode().PropertyValue());
            return pg;
        }

        public EventGen Event(Type handlerType, string name)
        {
            return CustomEvent(handlerType, name).WithStandardImplementation();
        }

        public EventGen CustomEvent(Type handlerType, string name)
        {
            EventGen eg = new EventGen(this, name, handlerType, mthVis | mthVirt | mthFlags);
            events.Add(eg);
            ResetAttrs();

            return eg;
        }

        public MethodGen Method(Type returnType, string name)
        {
            if (mthVis == 0)
                mthVis |= MethodAttributes.Private;
            if (tb.IsInterface)
                mthVirt |= MethodAttributes.Virtual | MethodAttributes.Abstract;

            MethodGen mg = new MethodGen(this, name, mthVis | mthVirt | mthFlags, returnType, implFlags);
            ResetAttrs();
            return mg;
        }

        public MethodGen ImplicitConversionFrom(Type fromType)
        {
            return ImplicitConversionFrom(fromType, "value");
        }

        public MethodGen ImplicitConversionFrom(Type fromType, string parameterName)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define conversion operators");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(tb, "op_Implicit").Parameter(fromType, parameterName);
        }

        public MethodGen ImplicitConversionTo(Type toType)
        {
            return ImplicitConversionTo(toType, "value");
        }

        public MethodGen ImplicitConversionTo(Type toType, string parameterName)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define conversion operators");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(toType, "op_Implicit").Parameter(tb, parameterName);
        }

        public MethodGen ExplicitConversionFrom(Type fromType)
        {
            return ExplicitConversionFrom(fromType, "value");
        }

        public MethodGen ExplicitConversionFrom(Type fromType, string parameterName)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define conversion operators");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(tb, "op_Explicit").Parameter(fromType, parameterName);
        }

        public MethodGen ExplicitConversionTo(Type toType)
        {
            return ExplicitConversionTo(toType, "value");
        }

        public MethodGen ExplicitConversionTo(Type toType, string parameterName)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot define conversion operators");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(toType, "op_Explicit").Parameter(tb, parameterName);
        }

        public MethodGen Operator(Operator op, Type returnType, Type operandType)
        {
            return Operator(op, returnType, operandType, "operand");
        }

        public MethodGen Operator(Operator op, Type returnType, Type operandType, string operandName)
        {
            if (op == null)
                throw new ArgumentNullException("op");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(returnType, "op_" + op.methodName).Parameter(operandType, operandName);
        }

        public MethodGen Operator(Operator op, Type returnType, Type leftType, Type rightType)
        {
            return Operator(op, returnType, leftType, "left", rightType, "right");
        }

        public MethodGen Operator(Operator op, Type returnType, Type leftType, string leftName, Type rightType, string rightName)
        {
            if (op == null)
                throw new ArgumentNullException("op");

            ResetAttrs();
            mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            mthVis = MethodAttributes.Public;
            return Method(returnType, "op_" + op.methodName)
                .Parameter(leftType, leftName)
                .Parameter(rightType, rightName)
                ;
        }

        public TypeGen Class(string name)
        {
            return Class(name, typeof(object), Type.EmptyTypes);
        }

        public TypeGen Class(string name, Type baseType)
        {
            return Class(name, baseType, Type.EmptyTypes);
        }

        public TypeGen Class(string name, Type baseType, params Type[] interfaces)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot contain nested types");

            if (typeVis == 0)
                typeVis |= TypeAttributes.NestedPrivate;

            TypeGen tg = new TypeGen(this, name, (typeVis | typeVirt | typeFlags | TypeAttributes.Class) ^ TypeAttributes.BeforeFieldInit, baseType, interfaces);
            ResetAttrs();
            return tg;
        }

        public TypeGen Struct(string name)
        {
            return Struct(name, Type.EmptyTypes);
        }

        public TypeGen Struct(string name, params Type[] interfaces)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot contain nested types");

            if (typeVis == 0)
                typeVis |= TypeAttributes.NestedPrivate;

            TypeGen tg = new TypeGen(this, name, (typeVis | typeVirt | typeFlags | TypeAttributes.Sealed | TypeAttributes.SequentialLayout) ^ TypeAttributes.BeforeFieldInit, typeof(ValueType), interfaces);
            ResetAttrs();
            return tg;
        }
        #endregion

        #region Interface implementations
        void DefineMethodOverride(MethodGen methodBody, MethodInfo methodDeclaration)
        {
            foreach (InterfaceImplEntry iie in implementations)
            {
                if (iie.InterfaceMethod.Member == methodDeclaration)
                {
                    iie.Bind(methodBody);
                    return;
                }
            }
        }

        public MethodGen MethodImplementation(Type interfaceType, Type returnType, string name)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot contain explicit interface implementations");

            MethodGen mg = new MethodGen(this, name,
                MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                returnType, 0);
            mg.ImplementedInterface = interfaceType;
            return mg;
        }

        public PropertyGen PropertyImplementation(Type interfaceType, Type type, string name)
        {
            if (tb.IsInterface)
                throw new InvalidOperationException("Interface cannot contain explicit interface implementations");

            PropertyGen pg = new PropertyGen(this,
                MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                type, name);
            pg.ImplementedInterface = interfaceType;
            return pg;
        }
        #endregion

        public Type GetCompletedType()
        {
            return GetCompletedType(false);
        }

        public Type GetCompletedType(bool completeIfNeeded)
        {
            if (type != null)
                return type;

            if (completeIfNeeded)
            {
                Complete();
                return type;
            }

            throw new InvalidOperationException("The type hasn't been completed yet");
        }

        public bool IsCompleted
        {
            get { return type != null; }
        }

        public void FlushDefinitionQueue()
        {
            // cannot use foreach, because it is possible that new objects
            // will be appended while completing the existing ones
            for (int i = 0; i < definitionQueue.Count; i++)
            {
                definitionQueue[i].EndDefinition();
            }
            definitionQueue.Clear();
        }

        public void FlushCompletionQueue()
        {
            // cannot use foreach, because it is possible that new objects
            // will be appended while completing the existing ones
            for (int i = 0; i < completionQueue.Count; i++)
            {
                completionQueue[i].Complete();
            }
            completionQueue.Clear();
        }

        public void Complete()
        {
            if (type != null)
                return;

            foreach (TypeGen nested in nestedTypes)
                nested.Complete();

            // ensure creation of default constructor
            EnsureDefaultConstructor();

            FlushDefinitionQueue();
            FlushCompletionQueue();

            // implement all interfaces
            foreach (InterfaceImplEntry iie in implementations)
            {
                if (!iie.IsBound && iie.InterfaceMethod.Name == "get_ReturnValue")
                {
                    
                }
                if (!iie.IsBound && iie.InterfaceMethod.Name!="get_ReturnValue")
                    throw new NotImplementedException(string.Format(null, "Method '{1}' of interface '{0}' is not implemented",
                        iie.InterfaceType, iie.InterfaceMethod.Member));

                tb.DefineMethodOverride(iie.BoundMethod.GetMethodBuilder(), (MethodInfo)iie.InterfaceMethod.Member);
            }

            // set indexer name
            if (indexerName != null)
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    typeof(DefaultMemberAttribute).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { indexerName });
                tb.SetCustomAttribute(cab);
            }

            AttributeGen.ApplyList(ref customAttributes, tb.SetCustomAttribute);

            type = tb.CreateType();

            TypeInfo.UnregisterProvider(tb);
        }

        public static implicit operator Type(TypeGen tg)
        {
            if (tg is GenTypeGen)
                return (tg as GenTypeGen).type;
            if (tg == null)
                return null;

            if (tg.type != null)
                return tg.type;

            return tg.tb;
        }

        public override string ToString()
        {
            return tb.FullName;
        }

        public void EnsureDefaultConstructor()
        {
            if (constructors.Count == 0 && tb.IsClass)
            {
                // create default constructor
                ResetAttrs();
                Public.Constructor().LockSignature();
            }
        }

        #region Member registration
        public void Register(ConstructorGen constructor)
        {
            if (constructor.IsStatic)
                return;

            if (constructor.ParameterCount == 0 && tb.IsValueType)
                throw new InvalidOperationException("Value types cannot have default constructors");

            constructors.Add(constructor);
        }

        public void Register(MethodGen method)
        {
            if (owner.AssemblyBuilder.EntryPoint == null && method.Name == "Main" && method.IsStatic && (
                method.ParameterCount == 0 ||
                (method.ParameterCount == 1 && method.ParameterTypes[0] == typeof(string[]))))
                owner.AssemblyBuilder.SetEntryPoint(method.GetMethodBuilder());

            // match explicit interface implementations
            if (method.ImplementedInterface != null)
            {
                foreach (IMemberInfo mi in TypeInfo.Filter(TypeInfo.GetMethods(method.ImplementedInterface), method.Name, false, false, true))
                {
                    if (ArrayUtils.Equals(mi.ParameterTypes, method.ParameterTypes))
                    {
                        DefineMethodOverride(method, (MethodInfo)mi.Member);
                        methods.Add(method);
                        return;
                    }
                }

                throw new MissingMethodException("Cannot find method");
            }

            methods.Add(method);
        }
        #endregion

        #region ITypeInfoProvider implementation
        IEnumerable<IMemberInfo> ITypeInfoProvider.GetConstructors()
        {
            EnsureDefaultConstructor();
            FlushDefinitionQueue();
            return constructors;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetFields()
        {
            FlushDefinitionQueue();
            return fields;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetProperties()
        {
            FlushDefinitionQueue();
            return properties;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetEvents()
        {
            FlushDefinitionQueue();
            return events;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetMethods()
        {
            FlushDefinitionQueue();
            return methods;
        }

        string ITypeInfoProvider.DefaultMember
        {
            get { return indexerName; }
        }
        #endregion
    }
}
