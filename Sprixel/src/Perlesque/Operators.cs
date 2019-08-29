using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TriAxis.RunSharp;
using System.Reflection;
using TriAxis.RunSharp.Operands;


namespace Sprixel
{
    public partial class Perlesque
    {
        public static void BuildOperators(Grammar g, ref int counter, string expressionName, string termName, string termishName)
        {
            var Counter = counter;

            //g.PrefixOperators["++", true, true] = new OperatorDetails("a", AssociativityType.Left, null);
            //g.PrefixOperators["--", true, true] = new OperatorDetails("a", AssociativityType.Left, null);
            //g.PostfixOperators["--", true, true] = new OperatorDetails("a", AssociativityType.Right, null);
            //g.PostfixOperators["++", true, true] = new OperatorDetails("a", AssociativityType.Right, null);
            g.AddInfixOperator("+", "Add");
            g.AddInfixOperator("~", "Add");
            g.AddInfixOperator("-", "Subtract");
            g.AddInfixOperator("*", "Multiply");
            g.AddInfixOperator("/", "Divide");
            g.AddInfixOperator("%", "Modulus");
            g.AddInfixOperator("<<", "LeftShift");
            g.AddInfixOperator(">>", "RightShift");
            g.AddInfixOperator("&&", "LogicalAnd");
            g.AddInfixOperator("||", "LogicalOr");
            g.AddInfixOperator("^^", "LogicalXor");
            g.AddInfixOperator("&", "BitwiseAnd");
            g.AddInfixOperator("|", "BitwiseOr");
            g.AddInfixOperator("^", "BitwiseXor");
            g.AddInfixMutator("+=", "AssignAdd");
            g.AddInfixMutator("-=", "AssignSubtract");
            g.AddInfixMutator("*=", "AssignMultiply");
            g.AddInfixMutator("/=", "AssignDivide");
            g.AddInfixMutator("%=", "AssignModulus");
            g.AddInfixMutator("<<=", "AssignLeftShift");
            g.AddInfixMutator(">>=", "AssignRightShift");
            g.AddInfixMutator("&=", "AssignAnd");
            g.AddInfixMutator("|=", "AssignOr");
            g.AddInfixMutator("^=", "AssignXor");
            g.AddInfixOperator("<", "LT");
            g.AddInfixOperator(">", "GT");
            g.AddInfixOperator("<=", "LE");
            g.AddInfixOperator(">=", "GE");
            g.AddInfixOperator("==", "EQ");
            g.AddInfixOperator("!=", "NE");
            //g.CircumfixOperators["(", true, true] = new OperatorDetails("a", AssociativityType.Left, null, ")");
            //g.PostCircumfixOperators["(", true, true] = new OperatorDetails("a", AssociativityType.Left, null, ")");

            g.InfixOperators["=", true, true] = new OperatorDetails("a", AssociativityType.Right, (Grammar gr, string optext, Operand left, Operand right) =>
            {
                var c = gr.CodeGen;
                var match = c["m"];
                c.AssignAdd(c["VC"], 1); // create a local to store the intermediate result
                //c.WriteLine("an assignment to " + left.Field("Result").Cast(typeof(Field)).Invoke("FieldName"));
                c.InvokeAssign(c["cg"][c["VC"].Invoke("ToString")], left.Field("Result"), "Assign", right.Field("Result"));
                c.If(right.Field("Result").Property("Type").Property("Name").Invoke("StartsWith", "_Closure").LogicalAnd(left.Field("Result").Property("Type").IsInstanceOf(typeof(Field))));
                {
                    // propagate the closure's type generator so type inference works
                    //c.WriteLine("found a " + c["current_scope"][varname].Field("Type").Property("Name"));
                    //c.WriteLine(c["m"]["myInitializer"].Field("Result").Property("Type"));
                    c.Assign(c["current_scope"][left.Field("Result").Cast(typeof(Field)).Invoke("FieldName")].Field("TypeGen"), c["GG"].Field("ClosuresByName")[right.Field("Result").Property("Type").Property("Name")].Field("IntGen"));
                }
                c.End();
                c.Assign(match.Field("Result"), c["cg"][c["VC"].Invoke("ToString")]);
            });

            counter = Counter;
        }
    }
}

