using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core {
    public class Header {
        public static List<string> Parse(string aInput, out string oName) {
            //TODO: add support for encodings
            //TODO: add support for parsing values into separate pieces
            int i = aInput.IndexOf(": ");
            oName = aInput.Substring(0, i);
            var xResult = new List<string>();
            xResult.Add(aInput.Substring(i + 2));
            return xResult;
        }
    }
}
