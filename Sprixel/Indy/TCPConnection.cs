using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Indy.Sockets.Core {
    public class TCPConnection : IDisposable {
        protected const int DefaultBufferSize = 2048;

        protected readonly Stream Stream;
        public readonly Socket Socket;
        public readonly BinaryReader BinaryReader;
        public readonly BinaryWriter BinaryWriter;

        // new ones based on the binaryreader, and writer - here so tehy can be passed out to other users externally
        // Later can make optimized ones against an intermediary buffered stream, need to keep both binary and text mixed for protocols like HTTP
        public readonly BinaryReaderReader TextReader;
        public readonly BinaryWriterWriter TextWriter;

        public readonly Encoding Encoding = Constants.DefaultEncoding;

        // Stream is buffered, it flushes when
        // someone reads, or in this case a manual flush
        public void FlushWrites() {
            Stream.Flush();
        }

        public TCPConnection(Socket aSocket) : this(aSocket, Constants.DefaultEncoding, DefaultBufferSize) {
        }
        public TCPConnection(Socket aSocket, Encoding aEncoding, int aBufferSize) {
            Socket = aSocket;
            Stream = new BufferedStream(new NetworkStream(Socket, true), aBufferSize);
            Encoding = aEncoding;
            Init(out BinaryReader, out TextReader, out BinaryWriter, out TextWriter);
        }

        public TCPConnection(Stream aStream) : this(aStream, Constants.DefaultEncoding) {
        }
        public TCPConnection(Stream aStream, Encoding aEncoding) {
            Stream = aStream;
            Encoding = aEncoding;
            Init(out BinaryReader, out TextReader, out BinaryWriter, out TextWriter);
        }

        protected void Init(out BinaryReader oBinaryReader, out BinaryReaderReader oTextReader, out BinaryWriter oBinaryWriter, out BinaryWriterWriter oTextWriter) {
            oBinaryReader = new BinaryReader(Stream, Encoding);
            oTextReader = new BinaryReaderReader(BinaryReader);
            oBinaryWriter = new BinaryWriter(Stream, Encoding);
            oTextWriter = new BinaryWriterWriter(BinaryWriter);
        }

        public void Dispose() {
            Stream.Dispose();
        }

        
    }
}
