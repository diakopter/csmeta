using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Indy.Sockets.Core {
    public class Response {
        protected int mNumeric = 0;
        public int Numeric {
            get { return mNumeric; }
        }

        public readonly List<string> Text;

        public Response() {
            Text = new List<string>();
        }

        public Response(int aNumeric, List<string> aText) {
            mNumeric = aNumeric;
            Text = aText;
        }

        public virtual bool Parse(string aText) {
            mNumeric = int.Parse(aText.Substring(0, 3));
            Text.Add(aText.Substring(4));
            // Dont use aText[3], it is possible for servers to rerturn just 3 characters only
            // and the 4th could be non existent
            return aText.Substring(3, 1) == " ";
        }

        // Since users usually want the numeric, we allow an implicit conversion to int
        public static implicit operator int(Response aValue) {
            return aValue.Numeric;
        }
    }
}
