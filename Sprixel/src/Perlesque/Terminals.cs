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
        public static void BuildTerminals(Grammar g, ref int counter, string expressionName, string termName, string termishName)
        {
            var Counter = counter;

            g.AddPattern("varname", finished(rep(alt(t("_"), new RangeChar((uint)'A', (uint)'Z'), new RangeChar((uint)'a', (uint)'z'), new RangeChar((uint)'0', (uint)'9')), 1, -1)));

            g.AddPattern("typeName", alt(
                finished(r(seq(
                    t("Callable["),
                    ows(),
                    t(":"),
                    ows(),
                    t("("),
                    ows(),
                    n("typeParamsList", p("typeParamsList")),
                    ows(),
                    t("-->"),
                    ows(),
                    n("returnType", p("typeName")),
                    ows(),
                    t(")"),
                    ows(),
                    t("]")
                ), (Grammar gr) => {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Result"), c["GG"].Invoke("ResolveClosureType", c["AG"], match));
                })),
                r(seq(
                    n("parametricTypeName", p("NamespaceAndClass")),
                    opt(seq(
                        t("["),
                        n("typeParamsList", p("typeParamsList")),
                        t("]")
                    ))
                ), (Grammar gr) => {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Try();
                    {
                        c.InvokeAssign(match.Field("Result"), c["GG"], "ResolveParametricType", c["AG"], c["IN"].Invoke("Match", match["parametricTypeName"]), match);
                    }
                    c.CatchAll();
                    {
                        c.Assign(c["assertion"], false);
                    }
                    c.Finally();
                    c.End();
                    //c.WriteLine(match.Field("Result").Property("ConstantValue"));
                }),
                new Assert())
            );

            g.AddPattern("typeParamsList", finished(seq(
                ows(),
                opt(seq(
                    n("nextTypeParam", p("typeName")),
                    opt(seq(
                        ows(),
                        t(","),
                        n("typeParamsList", p("typeParamsList"))))
                )),
                ows()
            )));

            //g.AddPattern("typeNameList", BuildCommaList(p("typeName"), "typeNameList"));

            g.AddPattern("ScalarRef", n("Scalar", seq(t("$"), n("JustTheVarName", p("varname")))));

            g.AddPattern("ArrayRef", n("Scalar", seq(t("@"), p("varname"))));

            g.AddPattern("ContextualRef", n("Scalar", seq(t("$*"), p("varname"))));

            g.AddPattern("decimalIntLiteral",
                finished(r(n("digits", seq(opt(t("-")), new RangeChar((uint)'0', (uint)'9'), opt(rep(alt(t("_"), new RangeChar((uint)'0', (uint)'9')), 1, -1)))),
                (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    var value = c["IN"].Invoke("Match", match["digits"]);
                    c.InvokeAssign(c["i3"], typeof(Int32), "Parse", value.Invoke("Replace", "_", ""));
                    c.Assign(match.Field("Result"), Exp.New(typeof(TriAxis.RunSharp.Operands.IntLiteral), typeof(int), c["i3"]));
                }))
            );

            g.AddPattern("stringLiteral", finished(alt(
                r(seq(t("'"), n("strLit", rep(alt(seq(new Not(t("'")), new Not(t("\\'")), new AnyChar()), t("\\'")), 1, -1)), t("'")),
                (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Result"), Exp.New(typeof(StringLiteral), c["IN"].Invoke("Match", match["strLit"]).Invoke("Replace", "\\'", "'")));
                }),
                r(seq(t("\""), n("strLit", rep(alt(seq(new Not(t("\"")), new Not(t("\\\"")), new AnyChar()), t("\\\"")), 1, -1)), t("\"")),
                (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Result"), Exp.New(typeof(StringLiteral), c["IN"].Invoke("Match", match["strLit"]).Invoke("Replace", "\\\"", "\"")));
                })
            )));

            g.AddPattern("NamespaceAndClass", finished(seq(
                p("varname"),
                opt(seq(t("::"), p("NamespaceAndClass"))))));

            g.AddPattern("ws", finished(rep(
                alt(
                    t(" "),
                    t("\t"),
                    r(alt(t(new String(new char[] { (char)13, (char)10 })), t(new String(new char[] { (char)10 })), t("\r"), t("\n")), (Grammar gr) => {
                        var c = gr.CodeGen;
                        c.If(c["o"] > c["last_position"]);
                        {
                            //c.WriteLine("passing line " + c["last_line"] + " at " + c["o"] + " with " + c["i"][c["o"] - 1] + " and " + c["i"][c["o"]]);
                            c.Increment(c["last_line"]);
                            c["last_position"] = c["o"];
                        }
                        c.End();
                    }),
                    seq(
                        t("#"),
                        rep(seq(new Not(t(new String(new char[] { (char)13, (char)10 }))), new Not(t(new String(new char[] { (char)10 }))), new Not(t("\r")), new Not(t("\n")), new AnyChar()), 0, -1)
                    )
                ), 1, -1
            )));

            g.AddPattern("statementSeps", finished(seq(
                ows(),
                t(";"),
                opt(p("statementSeps"))
            )));

            g.AddPattern("frameKeyword", r(seq(t("_cc")/*, new Lookahead(t(","))*/),(Grammar gr) =>{
                var c = gr.CodeGen;
                c.Assign(c["m"].Field("Result"), c["cg"].Invoke("This"));
            }));

            g.AddPattern("selfKeyword", r(seq(t("self"), new Not(p("varname"))), (Grammar gr) => {
                var c = gr.CodeGen;
                c.Assign(c["m"].Field("Result"), c["cg"].Invoke("This").Invoke("Field", "__csmeta_invocant"));
            }));

            counter = Counter;
        }

        public static Pattern BuildCommaList(Pattern pat, string resultPatternName) {
            return seq(
                ows(),
                pat,
                ows(),
                opt(seq(
                    t(","),
                    p(resultPatternName)
                )),
                ows()
            );
        }
    }
}

