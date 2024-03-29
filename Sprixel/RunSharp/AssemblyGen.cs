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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	public class AssemblyGen
	{
		AssemblyBuilder asm;
		ModuleBuilder mod;
		List<TypeGen> types = new List<TypeGen>();
		List<AttributeGen> assemblyAttributes;
		List<AttributeGen> moduleAttributes;
		string ns = null;

		public AssemblyBuilder AssemblyBuilder { get { return asm; } }
		public ModuleBuilder ModuleBuilder { get { return mod; } }

		public void AddType(TypeGen tg)
		{
			types.Add(tg);
		}

	public class NamespaceContext : IDisposable
		{
			AssemblyGen ag;
			string oldNs;

			public NamespaceContext(AssemblyGen ag)
			{
				this.ag = ag;
				this.oldNs = ag.ns;
			}

			public void Dispose()
			{
				ag.ns = oldNs;
			}
		}

		public IDisposable Namespace(string name)
		{
			NamespaceContext nc = new NamespaceContext(this);
			ns = Qualify(name);
			return nc;
		}

		string Qualify(string name)
		{
			if (ns == null)
				return name;
			else
				return ns + "." + name;
		}

		#region Modifiers
		TypeAttributes attrs;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Public { get { attrs |= TypeAttributes.Public; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Private { get { attrs |= TypeAttributes.NotPublic; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Sealed { get { attrs |= TypeAttributes.Sealed; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Abstract { get { attrs |= TypeAttributes.Abstract; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen NoBeforeFieldInit { get { attrs |= TypeAttributes.BeforeFieldInit; return this; } }
		#endregion

		#region Custom Attributes

		public AssemblyGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public AssemblyGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<AssemblyGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<AssemblyGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<AssemblyGen>.CreateAndAdd(this, ref assemblyAttributes, AttributeTargets.Assembly, type, args);
		}

		public AssemblyGen ModuleAttribute(AttributeType type)
		{
			BeginModuleAttribute(type);
			return this;
		}

		public AssemblyGen ModuleAttribute(AttributeType type, params object[] args)
		{
			BeginModuleAttribute(type, args);
			return this;
		}

		public AttributeGen<AssemblyGen> BeginModuleAttribute(AttributeType type)
		{
			return BeginModuleAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<AssemblyGen> BeginModuleAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<AssemblyGen>.CreateAndAdd(this, ref moduleAttributes, AttributeTargets.Module, type, args);
		}

		#endregion

		#region Types
		public TypeGen Class(string name)
		{
			return Class(name, typeof(object));
		}

		public TypeGen Class(string name, Type baseType)
		{
			return Class(name, baseType, Type.EmptyTypes);
		}

		public TypeGen Class(string name, Type baseType, params Type[] interfaces)
		{
			TypeGen tg = new TypeGen(this, Qualify(name), (attrs | TypeAttributes.Class) ^ TypeAttributes.BeforeFieldInit, baseType, interfaces);
			attrs = 0;
			return tg;
		}

		public TypeGen Struct(string name)
		{
			return Struct(name, Type.EmptyTypes);
		}

		public TypeGen Struct(string name, params Type[] interfaces)
		{
			TypeGen tg = new TypeGen(this, Qualify(name), (attrs | TypeAttributes.Sealed | TypeAttributes.SequentialLayout) ^ TypeAttributes.BeforeFieldInit, typeof(ValueType), interfaces);
			attrs = 0;
			return tg;
		}

		public TypeGen Interface(string name)
		{
			return Interface(name, Type.EmptyTypes);
		}

		public TypeGen Interface(string name, params Type[] interfaces)
		{
			TypeGen tg = new TypeGen(this, Qualify(name), (attrs | TypeAttributes.Interface | TypeAttributes.Abstract) & ~(TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit), null, interfaces);
			attrs = 0;
			return tg;
		}

		public DelegateGen Delegate(Type returnType, string name)
		{
			return new DelegateGen(this, Qualify(name), returnType, (attrs | TypeAttributes.Sealed) & ~(TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit));
		}
		#endregion

		#region Construction
		public AssemblyGen(string name) : this(name, false) {}
		public AssemblyGen(string name, bool debugInfo)
		{
			if (name.IndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar }) != -1 ||
				name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
				name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				// treat as filename
				Initialize(AppDomain.CurrentDomain, new AssemblyName(Path.GetFileNameWithoutExtension(name)), AssemblyBuilderAccess.RunAndSave,
					name, debugInfo);
			}
			else
			{
				Initialize(AppDomain.CurrentDomain, new AssemblyName(name), AssemblyBuilderAccess.Run, null, debugInfo);
			}
		}

		AssemblyBuilderAccess access;
		string fileName;

		public AssemblyGen(AppDomain domain, AssemblyName name, AssemblyBuilderAccess access, string fileName, bool symbolInfo)
		{
			Initialize(domain, name, access, fileName, symbolInfo);
		}

		void Initialize(AppDomain domain, AssemblyName name, AssemblyBuilderAccess access, string fileName, bool symbolInfo)
		{
			this.access = access;
			this.fileName = fileName;

			if (fileName == null && (access & AssemblyBuilderAccess.Save) != 0)
				throw new ArgumentNullException("fileName", "File name must be set when assembly is configured for saving");

            //Console.WriteLine(fileName ?? "empty filename");
            //Console.WriteLine(name.FullName + " " + name.Name + " " + name.ToString());
            if (fileName == null && name.ToString().Contains(Path.DirectorySeparatorChar.ToString()))
                asm = domain.DefineDynamicAssembly(new AssemblyName(Path.GetFileName(name.ToString())), access, Path.GetDirectoryName(name.ToString()));
            else 
            if (fileName == null)
				asm = domain.DefineDynamicAssembly(name, access);
			else
				asm = domain.DefineDynamicAssembly(name, access, Path.GetDirectoryName(fileName));

			if (fileName == null)
				mod = asm.DefineDynamicModule(name.Name, symbolInfo);
			else
				mod = asm.DefineDynamicModule(Path.GetFileName(fileName), Path.GetFileName(fileName), symbolInfo);
		}

		public void Save()
		{
			Complete();

            if ((access & AssemblyBuilderAccess.Save) != 0)
                asm.Save(Path.GetFileName(fileName));
		}

		public Assembly GetAssembly()
		{
			Complete();
			return asm;
		}

		public void Complete()
		{
			foreach (TypeGen tg in types)
				tg.Complete();

			AttributeGen.ApplyList(ref assemblyAttributes, asm.SetCustomAttribute);
			AttributeGen.ApplyList(ref moduleAttributes, mod.SetCustomAttribute);
		}
		#endregion
	}
}
