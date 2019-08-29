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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	using Operands;

	public interface ICodeGenContext : IMemberInfo, ISignatureGen, IDelayedDefinition, IDelayedCompletion
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Typical implementation invokes XxxBuilder.GetILGenerator() which is a method as well.")]
		ILGenerator GetILGenerator();

		Type OwnerType { get; }
	}

	public partial class CodeGen
	{
		ILGenerator il;
		ICodeGenContext context;
		ConstructorGen cg;
		bool chainCalled = false;
		bool reachable = true;
        public bool IsReachable()
        {
            return reachable;
        }

		bool hasRetVar = false, hasRetLabel = false;
		LocalBuilder retVar = null;
		Label retLabel;
		Stack<Block> blocks = new Stack<Block>();
		public Dictionary<string, Label> Labels = new Dictionary<string, Label>();
		public Dictionary<string, Operand> NamedLocals = new Dictionary<string, Operand>();
        public Dictionary<string, Label> MarkedLabels = new Dictionary<string, Label>();

        public int InstructionCount; // zero is the switchpoint
		public ILGenerator IL { get { return il; } }
		public ICodeGenContext Context { get { return context; } }

		public CodeGen(ICodeGenContext context)
		{
			this.context = context;
			this.cg = context as ConstructorGen;
			il = context.GetILGenerator();
		}

        public Operand NewObject(Type target) {
            return Exp.New(target);
        }

        public Operand InvokeStatic(Type target, string method, int flag, params Operand[] args) {
            if (target == null)
                throw new InvalidOperationException("method " + method + " invoked on object of null type; error.");
            return Static.Invoke(target, method, args);
        }

		/*public static CodeGen CreateDynamicMethod(string name, Type returnType, params Type[] parameterTypes, Type owner, bool skipVisibility)
		{
			DynamicMethod dm = new DynamicMethod(name, returnType, parameterTypes, owner, skipVisibility);
			return new CodeGen(method.GetILGenerator(), defaultType, method.ReturnType, method.IsStatic, parameterTypes);
		}

		public static CodeGen FromMethodBuilder(MethodBuilder builder, params Type[] parameterTypes)
		{
			return new CodeGen(builder.GetILGenerator(), builder.DeclaringType, builder.ReturnType, builder.IsStatic, parameterTypes);
		}

		public static CodeGen FromConstructorBuilder(ConstructorBuilder builder, params Type[] parameterTypes)
		{
			return new CodeGen(builder.GetILGenerator(), builder.DeclaringType, builder.ReturnType, builder.IsStatic, parameterTypes);
		}*/

		#region Arguments
		public Operand This()
		{
			if (context.IsStatic)
				throw new InvalidOperationException("Cannot use This() in a static method");

			return new _Arg(0, context.OwnerType);
		}

		public Operand Base()
		{
			if (context.IsStatic)
				return new _StaticTarget(context.OwnerType.BaseType);
			else
				return new _Base(context.OwnerType.BaseType);
		}

		int _ThisOffset { get { return context.IsStatic ? 0 : 1; } }

		public Operand PropertyValue()
		{
			Type[] parameterTypes = context.ParameterTypes;
			return new _Arg(_ThisOffset + parameterTypes.Length - 1, parameterTypes[parameterTypes.Length - 1]);
		}

		public Operand Arg(string name)
		{
			ParameterGen param = context.GetParameterByName(name);
			return new _Arg(_ThisOffset + param.Position - 1, param.Type);
		}
		#endregion

		#region Locals
		public Operand Local()
		{
			return new _Local(this);
		}

		public Operand Local(Operand init)
		{
			Operand var = Local();
			Assign(var, init);
			return var;
		}

		public Operand Local(Type type)
		{
			return new _Local(this, type);
		}

		public Operand Local(Type type, Operand init)
		{
			Operand var = Local(type);
			Assign(var, init);
			return var;
		}
		#endregion

		bool HasReturnValue
		{
			get
			{
				Type returnType = context.ReturnType;
				return returnType != null && returnType != typeof(void);
			}
		}

		void EnsureReturnVariable()
		{
			if (hasRetVar)
				return;

			retLabel = il.DefineLabel();
			if (HasReturnValue)
				retVar = il.DeclareLocal(context.ReturnType);
			hasRetVar = true;
		}

		public bool IsCompleted
		{
			get
			{
				return blocks.Count == 0 && !reachable && hasRetVar == hasRetLabel;
			}
		}

		public void Complete()
		{
            foreach (var l in Labels)
                if (!MarkedLabels.ContainsKey(l.Key))
                    throw new InvalidOperationException("Label " + l.Key + " not marked");

			if (blocks.Count > 0)
				throw new InvalidOperationException("Cannot complete code as there are some remaining opened blocks");

			if (reachable)
			{
				if (HasReturnValue)
					throw new InvalidOperationException(string.Format(null, "Method must provide a return value: {0}", context));
				else
					Return();
			}

			if (hasRetVar && !hasRetLabel)
			{
				il.MarkLabel(retLabel);
				if (retVar != null)
					il.Emit(OpCodes.Ldloc, retVar);
				il.Emit(OpCodes.Ret);
				hasRetLabel = true;
			}
		}

	public class _Base : _Arg
		{
			public _Base(Type type) : base(0, type) { }

			public override bool SuppressVirtual
			{
				get
				{
					return true;
				}
			}
		}

	public class _Arg : Operand
		{
			ushort index;
			Type type;

			public _Arg(int index, Type type)
			{
				this.index = checked((ushort)index);
				this.type = type;
			}

			public override void EmitGet(CodeGen g)
			{
				g.EmitLdargHelper(index);

				if (IsReference)
					g.EmitLdindHelper(Type);
			}

			public override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(index);
					g.EmitStindHelper(Type, value, allowExplicitConversion);
				}
				else
				{
					g.EmitGetHelper(value, Type, allowExplicitConversion);
					g.EmitStargHelper(index);
				}
			}

			public override void EmitAddressOf(CodeGen g)
			{
				if (IsReference)
				{
					g.EmitLdargHelper(index);
				}
				else
				{
					if (index <= byte.MaxValue)
						g.il.Emit(OpCodes.Ldarga_S, (byte)index);
					else
						g.il.Emit(OpCodes.Ldarga, index);
				}
			}

			bool IsReference { get { return type.IsByRef; } }

			public override Type Type
			{
				get
				{
					return IsReference ? type.GetElementType() : type;
				}
			}

			public override bool TrivialAccess
			{
				get
				{
					return true;
				}
			}
		}

		public class _Local : Operand
		{
			CodeGen owner;
			LocalBuilder var;
			Block scope;
			Type t, tHint;

			public _Local(CodeGen owner)
			{
				this.owner = owner;
				scope = owner.GetBlockForVariable();
			}
			public _Local(CodeGen owner, Type t)
			{
				this.owner = owner; this.t = t;
				scope = owner.GetBlockForVariable();
			}

			public _Local(CodeGen owner, LocalBuilder var)
			{
				this.owner = owner;
				this.var = var;
				this.t = var.LocalType;
			}

			void CheckScope(CodeGen g)
			{
				if (g != owner)
					throw new InvalidOperationException("The variable is accessed from an invalid context");
				if (scope != null && !owner.blocks.Contains(scope))
					throw new InvalidOperationException("The variable is accessed from an invalid scope");
			}

			public override void EmitGet(CodeGen g)
			{
				CheckScope(g);

				if (var == null)
					throw new InvalidOperationException("Variable used without having been initialized");

				g.il.Emit(OpCodes.Ldloc, var);
			}

			public override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
			{
				CheckScope(g);

				if (t == null)
					t = value.Type;

				if (var == null)
					var = g.il.DeclareLocal(t);

				g.EmitGetHelper(value, t, allowExplicitConversion);
				g.il.Emit(OpCodes.Stloc, var);
			}

			public override void EmitAddressOf(CodeGen g)
			{
				CheckScope(g);

				if (var == null)
				{
					RequireType();
					var = g.il.DeclareLocal(t);
				}

				g.il.Emit(OpCodes.Ldloca, var);
			}

			public override Type Type
			{
				get
				{
					RequireType();
					return t;
				}
			}

			void RequireType()
			{
				if (t == null)
				{
					if (tHint != null)
						t = tHint;
					else
						throw new InvalidOperationException("Variable accessed before it's type was defined");
				}
			}

			public override bool TrivialAccess
			{
				get
				{
					return true;
				}
			}

			public override void AssignmentHint(Operand op)
			{
				if (tHint == null)
					tHint = Operand.GetType(op);
			}
		}

	public class _StaticTarget : Operand
		{
			Type t;

			public _StaticTarget(Type t) { this.t = t; }

			public override Type Type
			{
				get
				{
					return t;
				}
			}

			public override bool IsStaticTarget
			{
				get
				{
					return true;
				}
			}
		}

		public Operand this[string localName] // Named locals support. 
		{
			get
			{
                Operand target;
                if (this.context.ContainsParameter(localName)) {
                    target = Arg(localName);
                    return target;
                } else {
                    if (!NamedLocals.TryGetValue(localName, out target))
                        throw new InvalidOperationException("Variable " + localName + " used without having been initialized.");
                    return target;
                }
			}
			set
			{
				Operand target;
                if (this.context.ContainsParameter(localName)) {
                    target = Arg(localName);
                    Assign(target, value);
                } else {
                    if (NamedLocals.TryGetValue(localName, out target))
                        // run in statement form; C# left-to-right evaluation semantics "just work"
                        Assign(target, value);
                    else
                        NamedLocals.Add(localName, Local(value));
                }
			}
		}

        public Operand this[string localName, Type type] // Named local typed declaration without initialization
        {
            get
            {
                Operand target = new _Local(this, IL.DeclareLocal(type));
                NamedLocals.Add(localName, target);
                return target;
            }
        }
	}
}
