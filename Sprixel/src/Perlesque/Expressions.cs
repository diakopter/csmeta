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
        public static void BuildExpressions(Grammar g, ref int counter, string expressionName, string termName, string termishName)
        {
            var Counter = counter;

            g.AddPattern("postTerm", n("postTerm", seq(n("term", p(termName)), alt(
                r(seq(
                    t("."),
                    n("methodName", p("varname")),
                    t("("),
                    n("args", opt(p("argsList"))),
                    t(")")
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(c["OpArray"], c["GG"].Invoke("BindArgs", 1, c["m"]["args"]));
                    c.If(c["IN"].Invoke("Match", match["methodName"]) == "new");
                    {
                        c.InvokeAssign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), typeof(Exp), "New", match["term"].Field("Result").Property("Type"), c["OpArray"]);
                    }
                    c.Else();
                    {
                        c.Assign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), match["term"].Field("Result").Invoke("InvokeOverride", c["IN"].Invoke("Match", match["methodName"]), c["OpArray"]));
                    }
                    c.End();
                }),
                r(seq(
                    t("."),
                    n("fieldName", p("varname"))
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.If(c["IN"].Invoke("Match", match["methodName"]) == "new");
                    {
                        c.InvokeAssign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), typeof(Exp), "New", match["term"].Field("Result").Property("Type"));
                    }
                    c.Else();
                    {
                        c.Try();
                        {
                            c.Assign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), match["term"].Field("Result").Invoke("Field", c["IN"].Invoke("Match", match["fieldName"])));
                        }
                        c.CatchAll();
                        {
                            c.Assign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), match["term"].Field("Result").Invoke("Property", c["IN"].Invoke("Match", match["fieldName"])));
                        }
                        c.End();
                    }
                    c.End();
                }),
                r(seq(t("["), ows(), n("indexer", p(expressionName)), ows(), t("]")), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    //c.Assign(c["OpArray"], Exp.NewArray(typeof(Operand), 1));
                    //c.Assign(c["OpArray"][0], match["indexer"].Field("Result"));
                    c.InvokeAssign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), match["term"].Field("Result"), "ApplyIndexer", match["indexer"].Field("Result"));
                }),
                r(empty(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), match["term"].Field("Result"));
                })
            ))));

            g.AddPattern(expressionName, r(n("InfixExpression",
                seq(
                ows(),
                r(
                n("NewLeftOperand", p("postTerm"))
                , (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["m"].Field("Result"), c["m"]["NewLeftOperand"].Field("Result"));
                })
                ,
                opt(
                    seq(
                        ows(),
                        n("InfixOp", new InfixOp()),
                        ows(),
                        n("RightOperand", p("postTerm")),
                        r(empty(), (Grammar gr) =>
                        {
                            var c = g.CodeGen;
                            var s = c["s"];
                            var o = c["o"];
                            var m = c["m"];
                            var endmarker = "ReduceInfix" + Counter++;

                            var found = c["IN"].Invoke("Match", m["InfixOp"]);
                            var left = m["NewLeftOperand"];
                            var right = m["RightOperand"];
                            //c.WriteLine("dispatching left " + c["IN"].Invoke("Match", left) + ": " + left.Field("Start") + " to " + left.Field("End") + " " + left.Field("Name"));
                            c.GotoFalse(right.IsInstanceOf(typeof(object)), endmarker);
                            //c.WriteLine("dispatching right " + c["IN"].Invoke("Match", right) + ": " + right.Field("Start") + " to " + right.Field("End") + " " + right.Field("Name"));
                            c.GotoFalse(found.IsInstanceOf(typeof(object)), endmarker);

                            c.Switch(found);

                            foreach (var target in g.InfixOperators.Keys)
                            {
                                var op = g.InfixOperators[target];
                                if (op.ReduceActionGen != null)
                                {
                                    c.Case(target);
                                    //c.WriteLine("dispatching infix<" + target + ">");
                                    op.ReduceActionGen(g, target, left, right);
                                }
                            }
                            c.End();
                            c.Label(endmarker);

                        })))
            )), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.Assign(c["m"].Field("Result"), c["m"]["InfixExpression"].Field("Result"));
                //c.WriteLine("type of expression at " + c["o"] + " : ");
                //c.Try();
                //c.WriteLine(c["m"].Field("Result").Invoke("GetType"));
                //c.CatchAll();
                //c.End();
            }))
            ;

            g.AddPattern(termishName, alt(
                seq(new PrefixOp(), p(expressionName)),
                //p("statementListBlock"),
                new CircumfixOp(p(expressionName)),
                r(p("ScalarRef"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    var varname = c["IN"].Invoke("Match", match["Scalar"]);
                    c.If(c["current_scope"].Invoke("ContainsKey", varname).LogicalNot());
                    {
                        //c.WriteLine(c["current_scope"].Invoke("KeysString"));
                        c.Throw(Exp.New(typeof(Exception), "usage of undeclared variable " + varname + " at offset " + c["o"]));
                    }
                    c.End();
                    c.If(c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName").EQ((c["current_typegen"]).Invoke("GetName")));
                    {
                        c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName")));
                    }
                    c.Else();
                    {
                        c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Field("FrameGen").Invoke("GetName")).Invoke("Field", c["current_scope"][varname].Field("MangledName")));
                    }
                    c.End();
                }),
                r(p("ContextualRef"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    var varname = c["IN"].Invoke("Match", match["Scalar"]);

                    c.InvokeAssign(c["s01"], c["TG_top"], "GetMangledContextual", c["current_typegen"], c["current_scope"], c["root_scope"], varname);

                    c.If(c["current_typegen"].Invoke("GetName") == "TopLevelFrame");
                    {
                        c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", "_ctxl_" + c["s01"]));
                    }
                    c.Else();
                    {
                        c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", "TopLevelFrame").Invoke("Field", "_ctxl_" + c["s01"]));
                    }
                    c.End();
                }),
                p("stringLiteral"),
                p("decimalIntLiteral"),
                p("frameKeyword"),
                p("selfKeyword"),
                p("staticInvocation"),
                p("subDeclaration"),
                p("typeLiteral")
            ));

            g.AddPattern("typeLiteral", r(n("literalTypeName", p("typeName")), (Grammar gr) => {
                var c = gr.CodeGen;
                c.Assign(c["m"].Field("Result"), c["m"]["literalTypeName"].Field("Result"));
            }));

            g.AddPattern(termName, finished(alt(
                seq(t("("), ows(), r(n("InnerExpr", p(expressionName)), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["m"].Field("Result"), c["m"]["InnerExpr"].Field("Result"));
                }), ows(), t(")")),
                p("subInvocation"),
                p(termishName)
            )));

            g.AddPattern("staticInvocation", n("invocation", r(seq(
                n("typeName", p("typeName")),
                t("."),
                n("methodName", p("varname")),
                t("("),
                n("args", opt(p("argsList"))),
                t(")")
            ), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var match = c["m"];
                c.Assign(c["type1"], match["typeName"].Field("Result").Cast(typeof(TypeLiteral)).Property("ConstantValue").Cast(typeof(Type)));
                //c.WriteLine(c["IN"].Invoke("Match", match["NamespaceAndClass"]) + " resolved to " + c["type1"]);
                c.If(c["IN"].Invoke("Match", match["methodName"]) == "new");
                {
                    c.InvokeAssign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), typeof(Exp), "New", c["type1"], c["GG"].Invoke("BindArgs", 1, c["m"]["args"]));
                }
                c.Else();
                {
                    //c.WriteLine("static method name is " + c["IN"].Invoke("Match", match["methodName"]));
                    c.Assign(match.Field("Parent").Cast(typeof(Match)).Field("Result"), c["cg"].Invoke("InvokeStatic", c["type1"], c["IN"].Invoke("Match", match["methodName"]), 1, c["GG"].Invoke("BindArgs", 1, c["m"]["args"])));
                    /*c.If(match.Field("Parent").Cast(typeof(Match)).Field("Result").Property("Type").Invoke("Equals", typeof(void)));
                    {
                        c.Invoke(match.Field("Parent").Cast(typeof(Match)).Field("Result"), "ForceEmit", c["cg"]);
                    }
                    c.Else();
                    {
                        c.Assign(c["operand01"], c["cg"][c["GG"].Invoke("GetNextEphemeral"), match.Field("Parent").Cast(typeof(Match)).Field("Result").Property("Type")]);
                        c.Invoke(c["cg"], "Assign", c["cg"][c["GG"].Invoke("GetLastEphemeral")], c["m"].Field("Result"));
                    }
                    c.End();*/
                }
                c.End();
            })));

            g.AddPattern("subInvocation", r(n("invocation", r(seq(
                n("subName", alt(p("varname"), p("ScalarRef"))),
                t("("),
                n("args", p("argsList")),
                t(")")
            ), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var match = c["m"]["invocation"];
                var varname = c["IN"].Invoke("Match", match["subName"]);

                c.Assign(c["OpArray"], c["GG"].Invoke("BindArgs", c["cg"], c["m"]["args"]));

                // increment the instruction count at parse time.
                c.Increment(c["cg"].Field("InstructionCount"));

                //c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Instruction"), -1);
                
                // using that unique instruction count, record a program-runtime assignment to this frame instance's instruction pointer slot.
                c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["cg"].Field("InstructionCount"));

                c.If(!c["current_scope"].Invoke("ContainsKey", varname));
                {
                    c.Throw(Exp.New(typeof(InvalidOperationException), varname + " has not been declared."));
                }
                c.End();

                c.If(c["current_scope"][varname].Field("Invokable").LogicalNot());
                {
                    c.Throw(Exp.New(typeof(InvalidOperationException), varname + " is not invokable"));
                }
                c.End();

                //c.WriteLine("looking up " + c["current_scope"][varname].Field("Type").Property("Name"));
                c.Assign(c["temp_typegen01"], c["GG"].Field("ClassesByName")[c["current_scope"][varname].Field("Type").Property("Name")]);

                // parse-time, store the return type of this invokable in a type variable named type1
                c.If(!c["GG"].Field("ReturnTypesByInterface").Invoke("ContainsKey", c["current_scope"][varname].Field("Type").Property("Name")));
                {
                    c.Assign(c["type1"], c["GG"].Field("ReturnTypesByInterface")[c["temp_typegen01"].Field("IntGen").Invoke("GetName")]);
                }
                c.Else();
                {
                    //c.WriteLine("found a return type for: " + c["current_scope"][varname].Field("TypeGen").Invoke("GetName"));
                    c.Assign(c["type1"], c["GG"].Field("ReturnTypesByInterface")[c["temp_typegen01"].Invoke("GetName")]);
                    //c.WriteLine("return type: " + c["type1"]);
                }
                c.End();

                // create a slot in this frame for the callsite's return value
                c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", c["type1"], "_csmeta_callsite_returnslot_" + c["cg"].Field("InstructionCount"));
                c.Invoke(c["current_typegen"].Field("FrameGen").Field("CloneGen"), "Assign", c["current_typegen"].Field("FrameGen").Field("CloneGen")["clone"].Invoke("Field", "_csmeta_callsite_returnslot_" + c["cg"].Field("InstructionCount")), c["current_typegen"].Field("FrameGen").Field("CloneGen").Invoke("This").Invoke("Field", "_csmeta_callsite_returnslot_" + c["cg"].Field("InstructionCount")));
                
                c.If(c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName").EQ((c["current_typegen"]).Invoke("GetName")));
                {
                    c.Invoke(c["cg"], "InvokeAssign", c["cg"]["next_frame"], c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName"))/*.Invoke("Cast", c["current_scope"][varname].Field("TypeGen").Field("IntGen"))*/, "Bind", c["OpArray"]);
                }
                c.Else();
                {
                    //c.WriteLine(c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName"));


                    c.Invoke(c["cg"], "InvokeAssign", c["cg"]["next_frame"], c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Field("FrameGen").Invoke("GetName")).Invoke("Field", c["current_scope"][varname].Field("MangledName"))/*.Invoke("Cast", c["current_scope"][varname].Field("TypeGen").Field("IntGen"))*/, "Bind", c["OpArray"]);

                    //c.Invoke(c["cg"], "InvokeAssign", c["cg"]["next_frame"], c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("Scope").Field("Typegen").Invoke("GetName")).Invoke("Field", c["current_scope"][varname].Field("MangledName"))/*.Invoke("Cast", c["current_scope"][varname].Field("TypeGen").Field("IntGen"))*/, "Bind", c["OpArray"]);
                }
                c.End();
                c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Callee"), c["cg"]["next_frame"]);
                c.Invoke(c["cg"], "Return", c["cg"]["next_frame"]);
                c.Assign(c["type1"], c["GG"].Invoke("ResolveReturnType", c["type1"]));
                c.Invoke(c["cg"], "Label", "inst" + c["cg"].Field("InstructionCount").Invoke("ToString"));
                //c.WriteLine("return intgen: " + c["type1"]);
                c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_csmeta_callsite_returnslot_" + c["cg"].Field("InstructionCount")), c["cg"].Invoke("This").Invoke("Field", "Callee").Invoke("Cast", c["type1"]).Invoke("Invoke", "GetReturnValue"));
                c.Assign(match.Field("Result"), c["cg"].Invoke("This").Invoke("Field", "_csmeta_callsite_returnslot_" + c["cg"].Field("InstructionCount")));
            })), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.Assign(c["m"].Field("Result"), c["m"]["invocation"].Field("Result"));
            }));

            g.AddPattern("argsList", n("argsList", seq(
                ows(),
                opt(seq(
                    n("nextArg", p(expressionName)),
                    opt(seq(
                        ows(),
                        t(","),
                        p("argsList")))
                )),
                ows()
            )));

            g.AddPattern("paramsList", n("paramsList", seq(
                ows(),
                opt(seq(
                    n("nextParam", p("param")),
                    opt(seq(
                        ows(),
                        t(","),
                        p("paramsList")))
                )),
                ows()
            )));

            g.AddPattern("param", seq(
                n("myTypeName", p("typeName")),
                p("ws"),
                n("myVarName", p("ScalarRef"))
            ));

            counter = Counter;
        }
    }
}

