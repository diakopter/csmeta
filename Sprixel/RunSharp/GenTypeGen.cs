/*
 * Copyright (c) 2010, Matthew Wilson
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
using System.Reflection;
using System.Reflection.Emit;
using Sprixel;

namespace TriAxis.RunSharp
{
    public class GenTypeGen : TypeGen, ITypeInfoProvider
    {
		public static bool Running_On_Mono = Type.GetType("System.MonoType") != null;
        Type ConstructedGenericType;
        Type GenericTypeDefinition;
        Type[] TypeParams;

        // constructedGenericType is *not* a RuntimeType; it's a TypeBuilderInstantiation, but we don't have access to that :(
        public GenTypeGen(Type constructedGenericType, Type genericTypeDefinition, Type[] typeParams) {
            this.name = constructedGenericType.FullName;
            this.type = constructedGenericType;
            ConstructedGenericType = constructedGenericType;
            GenericTypeDefinition = genericTypeDefinition;
            TypeParams = typeParams;
            constructors = new List<IMemberInfo>();
            fields = new List<IMemberInfo>();
            properties = new List<IMemberInfo>();
            events = new List<IMemberInfo>();
            methods = new List<IMemberInfo>();
            var props = new HashSet<string>();
            foreach (var ctor in genericTypeDefinition.GetConstructors()) {
                constructors.Add(new TypeInfo.StdMethodInfo(System.Reflection.Emit.TypeBuilder.GetConstructor(constructedGenericType, ctor)));
            }
            foreach (var ctor in genericTypeDefinition.GetFields()) {
                fields.Add(new TypeInfo.StdFieldInfo(System.Reflection.Emit.TypeBuilder.GetField(constructedGenericType, ctor)));
            }
            /*foreach (var ctor in genericTypeDefinition.GetProperties()) {
                Console.WriteLine(ctor.Name);
                //PropertyInfo ctor. 
                PropertyInfo prop;
                try {
                    prop = new GenericProperty(ctor.Name, constructedGenericType, genericTypeDefinition, typeParams, ctor.PropertyType);
                } catch (Exception e) {
                    prop = null;
                }
                properties.Add(new TypeInfo.StdPropertyInfo(prop));
            }*/
            foreach (var ctor in genericTypeDefinition.GetEvents()) {
                //events.Add(new TypeInfo.StdEventInfo(ctor));
            }
            foreach (var meth in genericTypeDefinition.GetMethods()) {
                TriAxis.RunSharp.TypeInfo.StdMethodInfo ti;
                try {
                    ti = new TypeInfo.StdMethodInfo(System.Reflection.Emit.TypeBuilder.GetMethod(constructedGenericType, meth));
                } catch (ArgumentException) { // mono, I think.
                    ti = new TypeInfo.StdMethodInfo(meth);
                }
                //Console.WriteLine(constructedGenericType + " : " + ti.Name);
                if ((ti.Name.StartsWith("get_") || ti.Name.StartsWith("set_")) && !props.Contains(ti.Name.TrimStart(new[] { 'g', 's' }))) {
                    var n = ti.Name.TrimStart(new[] { 'g', 's' });
                    MethodInfo getter = null;
                    MethodInfo setter = null;
                    if (Running_On_Mono) {
						//Console.WriteLine ("ON MONO");
	                        methods.Add(new TypeInfo.StdMethodInfo(getter = System.Reflection.Emit.TypeBuilder.GetMethod(constructedGenericType, genericTypeDefinition.GetMethod("g" + n))));
	                        methods.Add(new TypeInfo.StdMethodInfo(setter = System.Reflection.Emit.TypeBuilder.GetMethod(constructedGenericType, genericTypeDefinition.GetMethod("s" + n))));
					} else {
						//Console.WriteLine ("ON .NET");
						try {
	                        methods.Add(new TypeInfo.StdMethodInfo(getter = System.Reflection.Emit.TypeBuilder.GetMethod(constructedGenericType, genericTypeDefinition.GetMethod("g" + n))));
	                        methods.Add(new TypeInfo.StdMethodInfo(setter = System.Reflection.Emit.TypeBuilder.GetMethod(constructedGenericType, genericTypeDefinition.GetMethod("s" + n))));
	                    } catch (Exception e) {
						}
					}
                    props.Add(n);
                    var prop = new GenericProperty(n.Substring(3), constructedGenericType, genericTypeDefinition, typeParams, meth.ReturnType, getter, setter);
                    properties.Add(new TypeInfo.StdPropertyInfo(prop));
                } else {
                    methods.Add(ti);
                }
            }
            TypeInfo.RegisterProvider(constructedGenericType, this);
            //ResetAttrs();
        }
        IEnumerable<IMemberInfo> ITypeInfoProvider.GetConstructors() {
            return constructors;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetFields() {
            return fields;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetProperties() {
            return properties;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetEvents() {
            return events;
        }

        IEnumerable<IMemberInfo> ITypeInfoProvider.GetMethods() {
            return methods;
        }

        string ITypeInfoProvider.DefaultMember {
            get { return indexerName; }
        }

        public override string ToString() {
            return name;
        }
    }

    public class GenericProperty : PropertyInfo {
        public string _Name;
        public MethodInfo GetMethod;
        public MethodInfo SetMethod;
        public Type _PropertyType;
        public Type GenericType;
        public Type GenericTypeDefinition;
        public override Module Module {
            get {
                return GenericTypeDefinition.Module;
            }
        }
        public GenericProperty(string name, Type constructedGenericType, Type genericTypeDefinition, Type[] typeParams, Type propertyType, MethodInfo getter, MethodInfo setter) {
            _Name = name;
            _PropertyType = propertyType;
            GenericType = constructedGenericType;
            GenericTypeDefinition = genericTypeDefinition;
            GetMethod = getter;
            SetMethod = setter;
            //GetMethod = new GenericAccessor(name, constructedGenericType, genericTypeDefinition);
            //SetMethod = new GenericAccessor(name, constructedGenericType, genericTypeDefinition);
        }
        public override bool IsDefined(Type attributeType, bool inherit) {
            return true;
        }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException();
        }
        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException();
        }
        public override Type ReflectedType {
            get { throw new NotImplementedException("m"); }
        }
        public override Type DeclaringType {
            get { return GenericType; }
        }
        public override string Name {
            get { return _Name; }
        }
        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException("l");
        }
        public override bool CanWrite {
            get { return true; }
        }
        public override bool CanRead {
            get { return true; }
        }
        public override PropertyAttributes Attributes {
            get { return new PropertyAttributes(); }
        }
        public override ParameterInfo[] GetIndexParameters() {
            return new ParameterInfo[] { };
        }
        public override MethodInfo GetSetMethod(bool nonPublic) {
            return SetMethod;
        }
        public override MethodInfo GetGetMethod(bool nonPublic) {
            return GetMethod;
        }
        public override MethodInfo[] GetAccessors(bool nonPublic) {
            return new[] { SetMethod, GetMethod };
        }
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException("k");
        }
        public override Type PropertyType {
            get { return _PropertyType; }
        }
    }

    public class GenericAccessor : MethodInfo {

        public string _Name;
        public Type _DeclaringType;
        public Type GenericTypeDefinition;
        public override Module Module {
            get {
                return GenericTypeDefinition.Module;
            }
        }

        public GenericAccessor(string name, Type declaringType, Type genericTypeDefinition) {
            _Name = name;
            _DeclaringType = declaringType;
            GenericTypeDefinition = genericTypeDefinition;
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            throw new NotImplementedException("j");
        }
        public override object[] GetCustomAttributes(bool inherit) {
            throw new NotImplementedException("i");
        }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
            throw new NotImplementedException("h");
        }
        public override Type ReflectedType {
            get { throw new NotImplementedException("g"); }
        }
        public override Type DeclaringType {
            get { return _DeclaringType; }
        }
        public override string Name {
            get { return _Name; }
        }
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException("f");
        }
        public override MethodAttributes Attributes {
            get { return new MethodAttributes(); }
        }
        public override RuntimeMethodHandle MethodHandle {
            get { throw new NotImplementedException("e"); }
        }
        public override MethodImplAttributes GetMethodImplementationFlags() {
            throw new NotImplementedException("d");
        }
        public override ParameterInfo[] GetParameters() {
            throw new NotImplementedException("c");
        }
        public override MethodInfo GetBaseDefinition() {
            throw new NotImplementedException("b");
        }
        public override ICustomAttributeProvider ReturnTypeCustomAttributes {
            get { throw new NotImplementedException("a"); }
        }
    }
}
