// 
// UTF32String.cs
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
using System.IO;

namespace Sprixel {
    using UTF16 = UInt16;
    using UTF32 = UInt32;

    public struct UTF32String {
        public const UTF16 HI_SURROGATE_START = 0xD800;
        public const UTF16 HI_SURROGATE_END = 0xDBFF;
        public const UTF16 LO_SURROGATE_START = 0xDC00;
        public const UTF32 D65536 = 0x10000;
        public const UTF32 LEAD_OFFSET = unchecked((uint)HI_SURROGATE_START - (D65536 >> 10));
        public const UTF32 SURROGATE_OFFSET = unchecked((uint)(D65536 - (HI_SURROGATE_START << 10) - LO_SURROGATE_START));

        public UTF32[] Chars;
        public int StrLength;
        public int Length;
        public string CachedUTF16;

        public UTF32String(uint[] codepoints) {
            Chars = codepoints;
            Length = codepoints.Length;
            StrLength = Length; // not really, but who cares
            CachedUTF16 = null;
        }

        public int InjectFileContent(int offset, string injectionFile) {
            var innerString = new UTF32String(";"+File.ReadAllText(injectionFile));
            var innerCodepoints = innerString.Chars;
            var newCodepoints = new uint[Length + innerString.Length];
            var oldCodepoints = Chars;
            Array.Copy(oldCodepoints, newCodepoints, offset);
            Array.Copy(innerCodepoints, 0, newCodepoints, offset, innerString.Length);
            if (offset < Length)
                Array.Copy(oldCodepoints, offset, newCodepoints, offset + innerString.Length, Length - offset);
            Length += innerString.Length;
            Chars = newCodepoints;
            //Console.WriteLine(Match(new Match(null, 0, Length - 1)));
            return Length;
        }

        public UTF32String(string str) {
            CachedUTF16 = null;
            var length = StrLength = str.Length;
            var chars = new List<UTF32>(length);
            int count = 0;
            for (int offset = 0; offset < length; ++offset) {
                uint code = (uint)str[offset];
                chars.Add(HI_SURROGATE_START <= code && code <= HI_SURROGATE_END && offset < length
                    ? ((code - HI_SURROGATE_START) << 10) + ((uint)(str[++offset]) - LO_SURROGATE_START) + D65536
                    : code);
                ++count;
            }
            /*fixed (char* pfixed = str) { // unsafe edition.  This has not been algorithmically proven/verified.
                char* p = pfixed;
                char* pfirst = p;
                char* peof = pfixed + length;
                for (; p < peof; ++count) {
                    uint code = (uint)*p;
                    chars.Add(HI_SURROGATE_START <= code && code <= HI_SURROGATE_END && p < peof - 1
                        ? ((code - HI_SURROGATE_START) << 10) + ((uint)*(++p) - LO_SURROGATE_START) + D65536
                        : (uint)*p);
                    ++p;
                }
            }*/
            Length = count;
            Chars = chars.ToArray();
        }

        /// <summary>
        /// Return a representation in UTF-16 (CLR native encoding)
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            if (CachedUTF16 != null)
                return CachedUTF16;
            var sb = new StringBuilder(StrLength);
            foreach (var codepoint in Chars) {
                if (codepoint > 0xFFFF) {
                    sb.Append((Char)(LEAD_OFFSET + (codepoint >> 10)));
                    sb.Append((Char)(LO_SURROGATE_START + (codepoint & 0x3FF)));
                } else {
                    sb.Append((Char)codepoint);
                }
            }
            return CachedUTF16 = sb.ToString();
        }

        public string Match(Match m) {
            if (m == null)
                return null;
            var sb = new StringBuilder(StrLength);
            var start = m.Start;
            var end = m.End;
            for (var i = start; i < end; ++i ) {
                var codepoint = Chars[i];
                if (codepoint > 0xFFFF) {
                    sb.Append((Char)(LEAD_OFFSET + (codepoint >> 10)));
                    sb.Append((Char)(LO_SURROGATE_START + (codepoint & 0x3FF)));
                } else {
                    sb.Append((Char)codepoint);
                }
            }
            return sb.ToString();
        }
    }
}
