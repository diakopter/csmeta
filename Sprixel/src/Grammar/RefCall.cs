using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel {
    public class RefCall {
        public uint Done;
        public uint Notd;
        public uint Fail;
        public RefCall Last;

        public RefCall(uint done, uint notd, uint fail, RefCall last) {
            Done = done;
            Notd = notd;
            Fail = fail;
            Last = last;
        }
    }
}
