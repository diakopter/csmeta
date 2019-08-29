using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core.Exceptions {
    public class Exception : System.Exception {
        public Exception() : base() { }
        public Exception(string aMsg) : base(aMsg) { }
    }

    public class InvalidResponse : Exception {
        public readonly int ResponseCode;
        public readonly string ResponseText;
        public InvalidResponse(int aResponseCode, string aReponseText) : base() {
            ResponseCode = aResponseCode;
            ResponseText = aReponseText;
        }

        public override string Message {
            get {
                return ResponseCode + " " + ResponseText;
            }
        }
    }

    public class InvalidArgument : Exception {
        public readonly string ArgName;
        public readonly string ArgValue;
        public InvalidArgument(string aArgName, object aArgValue) : base() { 
        }

        public override string Message {
            get {
                return "Invalid argument value.\r\n"
                    + "Argument Name: " + ArgName + "\r\n"
                    + "Value: " + ArgValue;
            }
        }

    }
}
