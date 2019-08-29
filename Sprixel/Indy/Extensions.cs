using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core {
    public static class Extensions {
        // These are extension methods so they can be used on any TextReader,
        // but we still need our BinaryReaderReader as it uses members
        // for some functionaly, including this as this uses virtual methods
        // i.e. ReadLine
        public static void ReadLines(this TextReader aThis, List<string> aDest, string aTerminator) {
            string xLine;
            while ((xLine = aThis.ReadLine()) != aTerminator) {
                aDest.Add(xLine);
            }
        }

        public static Dictionary<string, List<string>> ReadHeaders(this TextReader aThis) {
            var xResult = new Dictionary<string, List<string>>();
            string xLine = aThis.ReadLine();
            string xHeader = xLine;
            while (xLine.Length > 0) {
                xLine = aThis.ReadLine();
                if (xLine.StartsWith("\t")) {
                    xHeader = xHeader + (" " + xLine.Remove(0, 1)).TrimEnd();
                } else {
                    string xName;
                    var xValues = Header.Parse(xHeader, out xName);
                    xResult.Add(xName, xValues);
                    xHeader = xLine;
                }
            }
            return xResult;
        }

        public static void ReadRFCLines(this TextReader aThis, List<string> aDest) {
            string xLine;
            while ((xLine = aThis.ReadLine()) != ".") {
                if ((xLine.Length > 1) && (xLine[0] == '.')) {
                    aDest.Add(xLine.Substring(1));
                } else {
                    aDest.Add(xLine);
                }
            }
        }
    }
}
