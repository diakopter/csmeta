using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core {
    public class BinaryReaderReader : TextReader {

        public BinaryReaderReader(BinaryReader aBaseReader) {
            if (aBaseReader == null) {
                throw new ArgumentNullException("aBaseReader");
            }
            mBaseReader = aBaseReader;
        }

        protected readonly BinaryReader mBaseReader;
        public BinaryReader BaseReader {
            get { return mBaseReader; }
        }

        public const string EOL = "\r\n";

        protected Encoding mEncoding = Constants.DefaultEncoding;
        public Encoding Encoding {
            get { return mEncoding; }
        }

        public override int Read() {
            return mBaseReader.Read();
        }

        // POP3 specifies lines can be up to 512. 
        // 1024 to be safe for other protocols
        // TODO: Might be issues here with RFC 822 contents
        //   Will probably need to update the catpure like VCL
        //   Make a separate readline so this one can stay optimized?
        protected int mMaxLineLength = 1024;
        protected int MaxLineLength {
            get { return mMaxLineLength; }
            set { mMaxLineLength = value; }
        }
        /// <summary>
        ///     This method reads a string from the stream ending in CRLF. 
        /// </summary>
        /// <returns>
        ///     Returns the read string without the CRLF. If the read 
        ///     operation fails before CRLF an empty line is returned.
        /// </returns>
        public override string ReadLine() {
            var xResult = new StringBuilder(128, mMaxLineLength);
            char xChar = ' ';
            while (true) {
                char xLastChar = xChar;
                xChar = mBaseReader.ReadChar();
                if ((xChar == 0x0A) && (xLastChar == 0x0D)) {
                    break;
                } else if (xChar != 0x0D) {
                    xResult.Append(xChar);
                }
            }
            return xResult.ToString();
        }
    }
}