using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core {
	public class BinaryWriterWriter : TextWriter {

		public BinaryWriterWriter(BinaryWriter aBaseWriter) {
			if (aBaseWriter == null) {
				throw new ArgumentNullException("aBaseWriter");
			}
			mBaseWriter = aBaseWriter;
		}

        // We use the encoding of the BaseWriter, but in TextWriter Encoding.Get is abstract
        // so we have to override it. Its ok though, as it appears TextWriter itself never uses it
        // directly but instead reserves it for descendants that override Write(char)
        public override Encoding Encoding {
            get { return null; }
		}

        protected readonly BinaryWriter mBaseWriter;

        // This isnt abstract in base, but its a null implementation
        // We override it to implement the functionality.
        // Seems odd - should be abstract in base especially sine Encoding.Get is abstract
		public override void Write(char aValue) {
			mBaseWriter.Write(aValue);
		}

	}
}