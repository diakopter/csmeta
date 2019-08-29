using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TriAxis.RunSharp;
using TriAxis.RunSharp.Operands;

// pasted from Reflector's dissambler! And some definite assignment bugs (in Reflector (or Visual Studio)) worked around.

namespace Sprixel {
    public class hi : Grammar.IParser {
        Match Grammar.IParser.Parse(Matcher M, UTF32String IN, int o, uint b, State s) {
            return null;
        }
        Grammar Grammar.IParser.Regen() {
            return new Grammar("hi", new PatternCall(new PatternRef("Exp")));
        }
    }
}
