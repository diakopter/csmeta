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
        public static void BuildControlFlow(Grammar g, ref int counter, string expressionName, string termName, string termishName)
        {
            var Counter = counter;

            g.AddPattern("statement", finished(alt(p("statementBlock"), p("statementNonBlock"))));

            g.AddPattern("optStatementList", seq(
                opt(p("statementList")),
                alt(
                    p("statementSeps"),
                    p("ws"),
                    empty()
                )
            ));

            g.AddPattern("optClassStatementList", seq(
                opt(p("classStatementList")),
                alt(
                    p("statementSeps"),
                    p("ws"),
                    empty()
                )
            ));

            g.AddPattern("ifBlock", seq(
                t("if"),
                p("ws"),
                r(
                    n("protasis", p(expressionName)), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["if_stack"], "Push", c["if_current"]);
                        c.Increment(c["if_counter"]);
                        c.Assign(c["if_current"], c["if_counter"]);
                        c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result"));
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertContextualsIn", c["current_scope"], c["cg"]);
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "Goto", "csmeta_endif_" + c["if_current"]);
                    c.Invoke(c["cg"], "End");
                }),
                ows(),
                r(alt(
                    p("ElsifChain"),
                    p("ElseBlock"),
                    empty()
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Label", "csmeta_endif_" + c["if_current"]);
                    c.Assign(c["if_current"], c["if_stack"].Invoke("Pop"));
                })
            ));

            g.AddPattern("whileBlock", seq(
                t("while"),
                p("ws"),
                r(n("protasis", p(expressionName)), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["if_stack"], "Push", c["if_current"]);
                        c.Increment(c["if_counter"]);
                        c.Assign(c["if_current"], c["if_counter"]);
                        c.Invoke(c["cg"], "Label", "csmeta_begif_" + c["if_current"]);
                        c.Invoke(c["cg"], "GotoFalse", c["m"]["protasis"].Field("Result"), "csmeta_endif_" + c["if_current"]);
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["cg"].Field("InstructionCount"));
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertLexicalsIn", c["current_scope"], c["cg"]);
                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertContextualsIn", c["current_scope"], c["cg"]);
                    c.Invoke(c["cg"], "Goto", "csmeta_begif_" + c["if_current"]);
                    c.Invoke(c["cg"], "Label", "csmeta_endif_" + c["if_current"]);
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Assign(c["if_current"], c["if_stack"].Invoke("Pop"));
                })
            ));

            g.AddPattern("repeatWhileStatement", seq(
                t("repeat"),
                p("ws"),
                r(new Lookahead(t("{")), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["loop_stack"], "Push", c["loop_current"]);
                    c.Increment(c["loop_counter"]);
                    c.Assign(c["loop_current"], c["loop_counter"]);
                    var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["current_scope"], "AddHere", c["loop_current"].Invoke("ToString") + "$$", Exp.New(typeof(FrameLocal), c["loop_current"].Invoke("ToString") + "$$", typeof(int), c["current_scope"]));
                    c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", typeof(int), c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["cg"], "Assign", state_local, 1);
                    c.Invoke(c["cg"], "While", state_local.Invoke("GE", 1));
                    c.Invoke(c["cg"], "If", state_local.Invoke("EQ", 1));
                    {
                        c.Invoke(c["cg"], "Assign", state_local, 2);
                        c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                    }
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_test");
                    c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_start");
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                }),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["last_scope"], c["current_scope"]);
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                }),
                ows(),
                n("while_until", alt(t("while"),t("until"))),
                p("ws"),
                n("protasis", p(expressionName)),
                r(ows(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_test");
                    c.If(c["IN"].Invoke("Match", c["m"]["while_until"]) == "while");
                    {
                        c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result"));
                    }
                    c.Else();
                    {
                        c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result").Invoke("LogicalNot"));
                    }
                    c.End();
                    {
                        c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                    }
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["cg"], "Break");
                    c.Assign(c["loop_current"], c["loop_stack"].Invoke("Pop"));
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertContextualsIn", c["last_scope"], c["cg"]);
                })
            ));

            g.AddPattern("repeatWhileBlock", seq(
                t("repeat"),
                p("ws"),
                t("while"),
                p("ws"),
                r(n("protasis", p(expressionName)), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["loop_stack"], "Push", c["loop_current"]);
                        c.Increment(c["loop_counter"]);
                        c.Assign(c["loop_current"], c["loop_counter"]);
                        var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                        c.Invoke(c["current_scope"], "AddHere", c["loop_current"].Invoke("ToString") + "$$", Exp.New(typeof(FrameLocal), c["loop_current"].Invoke("ToString") + "$$", typeof(int), c["current_scope"]));
                        c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", typeof(int), c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                        c.Invoke(c["cg"], "Assign", state_local, 1);
                        c.Invoke(c["cg"], "While", state_local.Invoke("GE", 1));
                        c.Invoke(c["cg"], "If", state_local.Invoke("EQ", 1));
                        {
                            c.Invoke(c["cg"], "Assign", state_local, 2);
                            c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                        }
                        c.Invoke(c["cg"], "End");
                        c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_test");
                        c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_start");
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_test");
                    c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result"));
                    {
                        c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                    }
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["cg"], "Break");
                    c.Assign(c["loop_current"], c["loop_stack"].Invoke("Pop"));
                    c.Invoke(c["cg"], "End");
                })
            ));

            g.AddPattern("repeatUntilBlock", seq(
                t("repeat"),
                p("ws"),
                t("until"),
                p("ws"),
                r(n("protasis", p(expressionName)), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["loop_stack"], "Push", c["loop_current"]);
                        c.Increment(c["loop_counter"]);
                        c.Assign(c["loop_current"], c["loop_counter"]);
                        var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                        c.Invoke(c["current_scope"], "AddHere", c["loop_current"].Invoke("ToString") + "$$", Exp.New(typeof(FrameLocal), c["loop_current"].Invoke("ToString") + "$$", typeof(int), c["current_scope"]));
                        c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", typeof(int), c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                        c.Invoke(c["cg"], "Assign", state_local, 1);
                        c.Invoke(c["cg"], "While", state_local.Invoke("GE", 1));
                        c.Invoke(c["cg"], "If", state_local.Invoke("EQ", 1));
                        {
                            c.Invoke(c["cg"], "Assign", state_local, 2);
                            c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                        }
                        c.Invoke(c["cg"], "End");
                        c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_test");
                        c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_start");
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "Label", c["loop_current"] + "loop_test");
                    c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result"));
                    {
                        c.Invoke(c["cg"], "Break");
                    }
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["cg"], "Goto", c["loop_current"] + "loop_start");
                    c.Assign(c["loop_current"], c["loop_stack"].Invoke("Pop"));
                    c.Invoke(c["cg"], "End");
                })
            ));

            g.AddPattern("unlessBlock", seq(
                t("unless"),
                p("ws"),
                r(
                    seq(
                        t("("),
                        ows(),
                        n("protasis", p(expressionName)),
                        ows(),
                        t(")")
                    ), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["if_stack"], "Push", c["if_current"]);
                        c.Increment(c["if_counter"]);
                        c.Assign(c["if_current"], c["if_counter"]);
                        c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result").Invoke("LogicalNot"));
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "Goto", "csmeta_endif_" + c["if_counter"]);
                    c.Invoke(c["cg"], "End");
                }),
                ows(),
                r(alt(
                    p("ElseBlock"),
                    empty()
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Label", "csmeta_endif_" + c["if_current"]);
                    c.Assign(c["if_current"], c["if_stack"].Invoke("Pop"));
                })
            ));

            g.AddPattern("untilBlock", seq(
                t("until"),
                p("ws"),
                r(
                    seq(
                        t("("),
                        ows(),
                        n("protasis", p(expressionName)),
                        ows(),
                        t(")")
                    ), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        c.Invoke(c["cg"], "While", c["m"]["protasis"].Field("Result").Invoke("LogicalNot"));
                        c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "End");
                })
            ));

            g.AddPattern("ElsifChain", seq(n("alternative", r(seq(
                ows(),
                t("elsif"),
                p("ws"),
                t("("),
                ows(),
                n("protasis", p(expressionName)),
                    ows(),
                    t(")")
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    //c.Invoke(c["cg"], "Else");
                    c.Invoke(c["if_stack"], "Push", c["if_current"]);
                    c.Increment(c["if_counter"]);
                    c.Assign(c["if_current"], c["if_counter"]);
                    c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result"));
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                })),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "Goto", "csmeta_endif_" + c["if_counter"]);
                    c.Invoke(c["cg"], "End");
                }),
                ows(),
                r(alt(
                    p("ElsifChain"),
                    p("ElseBlock"),
                    empty()
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Label", "csmeta_endif_" + c["if_current"]);
                    c.Assign(c["if_current"], c["if_stack"].Invoke("Pop"));
                })));

            g.AddPattern("ElseBlock", seq(n("alternative", r(seq(
                ows(),
                t("else"),
                ows()
                ), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    //c.Invoke(c["cg"], "Else");
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                })),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                }),
                ows()
            ));

            g.AddPattern("loopBlock", n("loopBlock", alt(seq(
                t("loop"),
                p("ws"),
                r(t("("),
                (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["loop_stack"], "Push", c["loop_current"]);
                    c.Increment(c["loop_counter"]);
                    c.Assign(c["loop_current"], c["loop_counter"]);
                    var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["current_scope"], "AddHere", c["loop_current"].Invoke("ToString") + "$$", Exp.New(typeof(FrameLocal), c["loop_current"].Invoke("ToString") + "$$", typeof(int), c["current_scope"]));
                    c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", typeof(int), c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["cg"], "Assign", state_local, 1);
                    //c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                }),
                opt(p("statement")),
                ows(),
                t(";"),
                opt(n("protasis", p(expressionName))),
                r(ows(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["cg"], "While", state_local.Invoke("GE", 1));
                    c.Invoke(c["cg"], "If", state_local.Invoke("EQ", 1));
                    {
                        c.Invoke(c["cg"], "Assign", state_local, 2);
                        c.Invoke(c["cg"], "Goto", "loop_entry" + c["loop_current"]);
                    }
                    c.Invoke(c["cg"], "End");
                    c.Invoke(c["cg"], "Label", "loop_tail" + c["loop_current"]);
                }),
                t(";"),
                opt(n("tailstatement", p("statement"))),
                ows(),
                r(t(")"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Label", "loop_entry" + c["loop_current"]);
                    c.If(c["m"].Invoke("HasOwnKey", "protasis").LogicalAnd(c["m"]["protasis"].Field("Result").IsInstanceOf(typeof(object))));
                    {
                        c.Invoke(c["cg"], "If", c["m"]["protasis"].Field("Result").Invoke("LogicalNot"));
                        {
                            c.Invoke(c["cg"], "Break");
                        }
                        c.Invoke(c["cg"], "End");
                    }
                    c.End();
                }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "Goto", "loop_tail" + c["loop_current"]);
                    c.Invoke(c["cg"], "Label", "loop_exit" + c["loop_current"]);
                    c.Invoke(c["cg"], "End");
                    c.Assign(c["loop_current"], c["loop_stack"].Invoke("Pop"));
                    //c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                })
            ),
            seq(
                t("loop"),
                r(p("ws"),
                (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["loop_stack"], "Push", c["loop_current"]);
                    c.Increment(c["loop_counter"]);
                    c.Assign(c["loop_current"], c["loop_counter"]);
                    var state_local = c["cg"].Invoke("This").Invoke("Field", c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["current_scope"], "AddHere", c["loop_current"].Invoke("ToString") + "$$", Exp.New(typeof(FrameLocal), c["loop_current"].Invoke("ToString") + "$$", typeof(int), c["current_scope"]));
                    c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", typeof(int), c["current_scope"][c["loop_current"].Invoke("ToString") + "$$"].Field("MangledName"));
                    c.Invoke(c["cg"], "Assign", state_local, 1);
                    //c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                    c.Invoke(c["cg"], "While", state_local.Invoke("EQ", 1));
                }),
                ows(),
                r(p("statementListBlock"), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Invoke(c["cg"], "End");
                    c.Assign(c["loop_current"], c["loop_stack"].Invoke("Pop"));
                    //c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                })
            ))));

            g.AddPattern("statementBlock", alt(
                p("ifBlock"),
                p("unlessBlock"),
                p("whileBlock"),
                p("untilBlock"),
                p("repeatWhileBlock"),
                p("repeatUntilBlock"),
                p("loopBlock"),
                p("statementListBlock")
            ));

            g.AddPattern("classStatementBlock", alt(
                p("methodDeclaration"),
                p("ifBlock"),
                p("unlessBlock"),
                p("whileBlock"),
                p("untilBlock"),
                p("repeatWhileBlock"),
                p("repeatUntilBlock"),
                p("loopBlock"),
                p("statementListBlock")
            ));

            g.AddPattern("statementListBlock", seq(
                t("{"),
                p("optStatementList"),
                t("}")
            ));

            g.AddPattern("classStatementListBlock", seq(
                t("{"),
                ows(),
                opt(t("...")),
                p("optClassStatementList"),
                t("}")
            ));

            BuildStatementList(g, "statementList", p("statementBlock"), p("statementNonBlock"), p("optStatementList"));

            BuildStatementList(g, "classStatementList", p("classStatementBlock"), p("classStatementNonBlock"), p("optClassStatementList"));

            g.AddPattern("returnStatement", n("returnStatement", seq(
                t("return"),
                p("ws"),
                r(n("retValue", p(expressionName)), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.Assign(c["current_typegen"].Property("CRT"), c["m"]["retValue"].Field("Result").Type);
                    c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Return"), c["m"]["retValue"].Field("Result"));
                    c.Invoke(c["cg"], "Goto", "csmeta_return_label");
                })
            )));

            g.AddPattern("useStatement", n("useStatement", seq(
                t("use"),
                p("ws"),
                r(seq(n("filename", p("stringLiteral"))), (Grammar gr) => {
                    var c = gr.CodeGen;
                    c.Assign(c["l"], c["IN"].Invoke("InjectFileContent", c["o"], c["m"]["filename"].Field("Result").Cast(typeof(StringLiteral)).Property("ConstantValue").Cast(typeof(string))));
                    c.Assign(c["i"], c["IN"].Field("Chars"));
                })
            )));

            g.AddPattern("statementNonBlock", alt(
                p("useStatement"),
                p("gotoStatement"),
                p("nextStatement"),
                p("lastStatement"),
                p("repeatWhileStatement"),
                p("returnStatement"),
                p("myDeclaration"),
                p("contextualAssignment"),
                p("subDeclaration"),
                p("classPreDeclaration"),
                p("classDeclaration"),
                r(p(expressionName), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    c.If(c["m"].Field("Result").IsInstanceOf(typeof(Operand)));
                    {
                        c.If(c["m"].Field("Result").Property("Type").Invoke("Equals", typeof(void)));
                        {
                            c.Invoke(c["m"].Field("Result"), "ForceEmit", c["cg"]);
                        }
                        c.Else();
                        {
                            c.Assign(c["operand01"], c["cg"][c["GG"].Invoke("GetNextEphemeral"), c["m"].Field("Result").Property("Type")]);
                            c.Invoke(c["cg"], "Assign", c["cg"][c["GG"].Invoke("GetLastEphemeral")], c["m"].Field("Result"));
                        }
                        c.End();
                    }
                    c.End();
                })
            ));

            g.AddPattern("classStatementNonBlock", alt(
                p("fieldDeclaration"),
                p("gotoStatement"),
                p("nextStatement"),
                p("lastStatement"),
                p("repeatWhileStatement"),
                // p("returnStatement"), // return statement not allowed in class declaration
                p("myDeclaration"),
                p("contextualAssignment"),
                p("subDeclaration"),
                r(p(expressionName), (Grammar gr) => {
                    var c = gr.CodeGen;
                    c.If(c["m"].Field("Result").IsInstanceOf(typeof(Operand)));
                    {
                        c.If(c["m"].Field("Result").Property("Type").Invoke("Equals", typeof(void)));
                        {
                            c.Invoke(c["m"].Field("Result"), "ForceEmit", c["cg"]);
                        }
                        c.Else();
                        {
                            c.Assign(c["operand01"], c["cg"][c["GG"].Invoke("GetNextEphemeral"), c["m"].Field("Result").Property("Type")]);
                            c.Invoke(c["cg"], "Assign", c["cg"][c["GG"].Invoke("GetLastEphemeral")], c["m"].Field("Result"));
                        }
                        c.End();
                    }
                    c.End();
                })
            ));

            g.AddPattern("optLabelMarker", opt(r(seq(n("labelName", p("varname")), t(":"), p("ws")), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var labelName = c["IN"].Invoke("Match", c["m"]["labelName"]);
                c.Invoke(c["cg"], "Label", labelName);
            })));

            g.AddPattern("gotoStatement", alt(r(seq(t("goto"), p("ws"), n("labelName", p("varname"))), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var labelName = c["IN"].Invoke("Match", c["m"]["labelName"]);
                c.Invoke(c["cg"], "Goto", labelName);
            }),
            r(seq(t("goto"), p("ws"), n("continuation", p(termName))), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.Invoke(c["cg"], "Return", c["m"]["continuation"].Field("Result"));
            })));

            g.AddPattern("lastStatement", r(seq(t("last"), new Not(p("varname")), new Lookahead(new AnyChar())), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.Invoke(c["cg"], "Break");
            }));

            g.AddPattern("nextStatement", alt(r(seq(t("next"), new Not(p("varname")), new Lookahead(new AnyChar())), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.Invoke(c["cg"], "Continue");
            }),
            r(seq(t("next"), p("ws"), n("labelName", p("varname"))), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var labelName = c["IN"].Invoke("Match", c["m"]["labelName"]);
                c.Invoke(c["cg"], "Goto", labelName);
            })));

            g.AddPattern("stmtCommas", seq(
                ows(),
                p("statement"),
                ows(),
                opt(seq(
                    t(","),
                    p("stmtCommas"))),
                ows()
            ));

            counter = Counter;
        }

        public static void BuildStatementList(Grammar g, string patternName, Pattern statementBlock, Pattern statementNonBlock, Pattern optionalEdition) {
            g.AddPattern(patternName, seq(
                rep(alt(p("ws"), p("statementSeps")), 0, -1),
                p("optLabelMarker"),
                rep(alt(p("ws"), p("statementSeps")), 0, -1),
                alt(
                    seq(
                        n("statement", statementBlock),
                        opt(seq(
                            alt(
                                seq(
                                    lb("}"),
                                    t("\n"),
                                    opt(p("statementSeps"))),
                                p("statementSeps")),
                            optionalEdition))),
                    seq(
                        n("statement", statementNonBlock),
                        opt(seq(
                            alt(
                                seq(
                                    t("\n"),
                                    opt(p("statementSeps"))),
                                p("statementSeps")),
                            optionalEdition.Regen(g))))),
                rep(alt(p("ws"), p("statementSeps")), 0, -1)
            ));
        }
    }
}

