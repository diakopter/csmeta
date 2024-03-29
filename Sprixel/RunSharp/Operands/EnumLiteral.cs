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
using System.Text;

namespace TriAxis.RunSharp.Operands
{
public class EnumLiteral : Operand
	{
		Enum value;

		public EnumLiteral(Enum value) { this.value = value; }

		public override void EmitGet(CodeGen g)
		{
			Type t = Enum.GetUnderlyingType(Type);
			if (t == typeof(long))
				g.EmitI8Helper(Convert.ToInt64(value, null), true);
			else if (t == typeof(ulong))
				g.EmitI8Helper(unchecked((long)Convert.ToUInt64(value, null)), false);
			else if (t == typeof(uint))
				g.EmitI4Helper(unchecked((int)Convert.ToUInt32(value, null)));
			else
				g.EmitI4Helper(Convert.ToInt32(value, null));
		}

		public override Type Type
		{
			get
			{
				return value.GetType();
			}
		}

		public override object ConstantValue
		{
			get
			{
				return value;
			}
		}
	}
}
