using System;
using System.IO;

namespace Sprixel
{
	public class InputSource
	{
		public string UTF16input;
		public Stream InboundInput;
		
		public InputSource (string utf16input)
		{
			UTF16input = utf16input;
		}
		
		public override string ToString ()
		{
			return UTF16input;
		}
	}
}
