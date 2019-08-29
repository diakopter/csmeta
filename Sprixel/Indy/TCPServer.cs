using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Indy.Sockets.Core {
    public class TCPServer : IDisposable {
        public delegate void TCPServerRunDelegate(TCPConnection aConnection);
        protected TCPServerRunDelegate mDoRun;
        protected TcpListener mListener;

        // This is the quick and dirty form. 
        // Later I will add other methods for 
        // multiple bindings and for IP binding.
        public TCPServer(int aPort, TCPServerRunDelegate aDoRun) {
            mDoRun = aDoRun;
            mListener = new TcpListener(IPAddress.Any, aPort);
            mListener.Start();
            var xThread = new Thread(ListenerThreadRun);
            xThread.Start();
        }

        protected void ConnectionThreadRun(object aSocket) {
            var xConnection = new TCPConnection((Socket)aSocket);
            mDoRun(xConnection);
        }

        protected void ListenerThreadRun() {
            while (true) {
                var xSocket = mListener.AcceptSocket();
                var xThread = new Thread(ConnectionThreadRun);
                xThread.Start(xSocket);
            }
        }

        public void Dispose() {
            mListener.Stop();
        }
    }
}
