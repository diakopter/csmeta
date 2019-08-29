using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Indy.Sockets.Core.Exceptions;

namespace Indy.Sockets.Core {
    /// <summary>
    ///     This is the base class for all other tcp based clients.  It exposes very little
    ///     as it is meant to be inherited even for very simple protocols.
    /// </summary>
    public abstract class TCPClient : IDisposable {
        protected readonly TCPConnection mConnection;

        /// <summary>
        ///     This method should always be overriden by any descendents of TCPClient.  When
        ///     overriding this method you should return the default port number standard to
        ///     the protocol that you are using.  If this virtual method is not overiden the
        ///     method will return an invalid port number of -1, causing an exception when
        ///     other internal methods call this method to assign a port number or when the
        ///     system attempts the connection.
        /// </summary>
        /// <returns>An integer that represents the default port number for the protocol.</returns>
        protected virtual int GetDefaultPort() {
            return -1;
        }

        /// <summary>
        ///     This constructor simplifies the process of correctly creating a socket,
        ///     connecting and creating an instance of TCPConnection, where the stream
        ///     and other I/O properties are exposed and managed.  The constructor simplifies
        ///     descendents by accepting only one parameter where both the host and port can
        ///     be specified, allowing the developer to avoid constructor hell.
        /// </summary>
        /// <param name="aHost">
        ///     This string parameter can contain both the host and port, or just
        ///     the host name.  When including the port with the host use a ":" to seperate
        ///     the two, eg "localhost:80".
        /// </param>
        public TCPClient(string aHost) {
            Socket xSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int xPort;
            string[] xHostParts = aHost.Split(':');
            if (xHostParts.Length == 2) {
                xPort = int.Parse(xHostParts[1]);
            } else {
                xPort = GetDefaultPort();
                if (xPort == -1) {
                    throw new Exceptions.Exception("No default port specified.");
                }
            }
            xSocket.Connect(new IPEndPoint(ResolveAddress(xHostParts[0]), xPort));
            mConnection = new TCPConnection(xSocket);    
        }

        // This is separate as we need to encapsulate more than FCL offers us
        // It is here so it is reusable by all
        // It only returns the first IP, that is by design. If you want the full
        // list, use the FCL methods directly.
        protected IPAddress ResolveAddress(string aAddress) {
            IPHostEntry xIP = Dns.GetHostEntry(aAddress);
            // Some host names (ie localhost) can return mult entries
            // For now we want the first IP4 one, which is not always in [0]
            return xIP.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        public Response Do(string aCmd, params int[] aAllowedResponseCodes) {
            mConnection.TextWriter.WriteLine(aCmd);
            return ReadResponse(aAllowedResponseCodes);
        }

        public Response Do(string aCmd, string aParam, params int[] aAllowedResponseCodes) {
            mConnection.TextWriter.WriteLine((aCmd + " " + aParam).TrimEnd());
            return ReadResponse(aAllowedResponseCodes);
        }

        // Usage: Do("USER", new string[]{aUser}, ResponsePOP3.OK);
        public Response Do(string aCmd, object[] aParams, params int[] aAllowedResponseCodes) {
            var xParams = new StringBuilder();
            foreach (object o in aParams) {
                xParams.Append(" " + o.ToString());
            }
            mConnection.TextWriter.WriteLine((aCmd + xParams.ToString()).TrimEnd());
            return ReadResponse(aAllowedResponseCodes);
        }

        protected virtual Response NewResponse() {
            return new Response();
        }

        protected Response ReadResponse(params int[] aPermittedNumericResponses) {
            var xResult = NewResponse();
            string xText;
            do {
                xText = mConnection.TextReader.ReadLine();
            } while (!xResult.Parse(xText));
            if (!aPermittedNumericResponses.Contains(xResult.Numeric)) {
                //TODO: Change to use Aggregate or other to get string
                // Possibly make an extension method for List<> or a custom text class
                // inheriting from List<>
                var xAllText = new StringBuilder();
                foreach (var x in xResult.Text) {
                    xAllText.AppendLine(x);
                }
                throw new InvalidResponse(xResult.Numeric, xAllText.ToString());
            }
            return xResult;
        }

        /// <summary>
        ///     This constructor is here to give the developer more control in the creation
        ///     of there TCPConnection.  Once having created the TCPConnection it is passed
        ///     as a parameter into the consctuctior.
        /// </summary>
        /// <param name="aConnection">TCPConnection for the TCPClient</param>
        public TCPClient(TCPConnection aConnection) {
            mConnection = aConnection;
        }

        public virtual void Close() {
            mConnection.Dispose();
        }

        // This is the "Friendly" disconnect with command
        // Protocols should override this, send the proper quit
        // command then call base.Disconnect();
        public virtual void Disconnect() {
            Close();
        }

        // Differently than VCL because Dispose is "optional" and called
        // in "good" circumstances
        public void Dispose() {
            Disconnect();
        }
    }
}
