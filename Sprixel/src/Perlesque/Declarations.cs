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
        public static void BuildDeclarations(Grammar g, ref int counter, string expressionName, string termName, string termishName)
        {
            var Counter = counter;

            g.AddPattern("myDeclaration", seq(r(seq(
                t("my"),
                p("ws"),
                opt(seq(n("myTypeName", p("typeName")),
                p("ws"))),
                n("myVarName", p("ScalarRef")),
                ows()
                //,d("found a my declaration")
            ), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                var varname = c["IN"].Invoke("Match", c["m"]["myVarName"]);
                var typename = c["IN"].Invoke("Match", c["m"]["myTypeName"]);
                c.Try();
                {
                    c.If(c["m"]["myTypeName"].Field("Success").LogicalAnd(c["m"]["myTypeName"].Field("Result").IsNotNull()));
                    {
                        c.If(c["m"]["myTypeName"].Field("Result").IsNotNull().LogicalAnd(c["m"]["myTypeName"].Field("Result").Cast(typeof(TypeLiteral)).IsNotNull()));
                        {
                            c.Assign(c["type1"], c["m"]["myTypeName"].Field("Result").Cast(typeof(TypeLiteral)).Property("ConstantValue"));
                        }
                        c.Else();
                        {
                            c.InvokeAssign(c["type1"], c["GG"], "ResolveType", typename);
                        }
                        c.End();
                        //c.WriteLine(c["type1"]);
                        c.Invoke(c["current_scope"], "AddHere", varname, Exp.New(typeof(FrameLocal), varname, c["type1"], c["current_scope"]));
                        c.If(c["m"]["myTypeName"].Field("Result").IsNotNull().LogicalAnd(c["m"]["myTypeName"].Field("Result").Cast(typeof(TypeLiteral)).IsNotNull()).LogicalAnd(c["m"]["myTypeName"].Field("Result").Cast(typeof(TypeLiteral)).Property("ConstantValue").Cast(typeof(Type)).Property("Name").Invoke("StartsWith", "_IClosure")));
                        {
                            c.Assign(c["current_scope"][varname].Field("Invokable"), true);
                        }
                        c.End();
                        c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", c["type1"], c["current_scope"][varname].Field("MangledName"));
                        c.Invoke(c["current_typegen"].Field("FrameGen").Field("CloneGen"), "Assign", c["current_typegen"].Field("FrameGen").Field("CloneGen")["clone"].Invoke("Field", c["current_scope"][varname].Field("MangledName")), c["current_typegen"].Field("FrameGen").Field("CloneGen").Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName")));
                    }
                    c.End();
                }
                c.CatchAll();
                {
                    c.Assign(c["assertion"], false);
                    //c.Throw(Exp.New(typeof(Exception), "parse failure; got to line " + c["last_line"] + ", or incorrect redeclaration of " + varname + " at offset " + c["o"]));
                }
                c.End();
                //c.WriteLine("declared " + varname + " of type " + typename + " at " + c["o"]);
            }),
                new Assert(),
                alt(r(seq(
                    t("="),
                    ows(),
                    n("myInitializer", p(expressionName))
                //,d("found an initializer")
                    ), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        var varname = c["IN"].Invoke("Match", c["m"]["myVarName"]);
                        c.If(c["current_scope"].Invoke("HasOwnKey", varname).LogicalNot());
                        {
                            c.Assign(c["type1"], c["m"]["myInitializer"].Field("Result").Property("Type"));
                            c.If(c["type1"].Property("Name").Invoke("StartsWith", "_Closure"));
                            {
                                c.Assign(c["type1"], c["GG"].Field("ClosuresByName")[c["type1"].Property("Name")].Field("IntGen"));
                            }
                            c.End();
                            c.Invoke(c["current_scope"], "AddHere", varname, Exp.New(typeof(FrameLocal), varname, c["type1"], c["current_scope"]));
                            //c.WriteLine("declared local with initializer of type " + c["type1"].Property("FullName"));
                            c.If(c["type1"].Property("Name").Invoke("StartsWith", "_IClosure"));
                            {
                                c.Assign(c["current_scope"][varname].Field("TypeGen"), c["GG"].Field("ClosuresByName")[c["type1"].Property("Name")]);
                                //c.WriteLine("gave " + varname + " the typegen " + c["GG"].Field("ClosuresByName")[c["type1"].Property("Name")]);
                                c.Assign(c["current_scope"][varname].Field("TypeGen").Field("IntGen"), c["GG"].Field("ClosuresByName")[c["type1"].Property("Name")]);
                                c.Assign(c["current_scope"][varname].Field("Invokable"), true);
                            }
                            c.End();
                            c.InvokeAssign(c["temp_fieldgen01"], c["current_typegen"].Field("FrameGen"), "PublicField", c["type1"], c["current_scope"][varname].Field("MangledName"));
                            c.Invoke(c["current_typegen"].Field("FrameGen").Field("CloneGen"), "Assign", c["current_typegen"].Field("FrameGen").Field("CloneGen")["clone"].Invoke("Field", c["current_scope"][varname].Field("MangledName")), c["current_typegen"].Field("FrameGen").Field("CloneGen").Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName")));
                        }
                        c.Else();
                        {
                            c.If(c["type1"].Property("Name").Invoke("StartsWith", "_Closure").LogicalOr(c["type1"].Property("Name").Invoke("StartsWith", "_IClosure")));
                            {
                                c.Assign(c["current_scope"][varname].Field("Invokable"), true);
                            }
                            c.End();
                        }
                        c.End();
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName")), c["m"]["myInitializer"].Field("Result"));
                        c.Assign(c["m"].Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["current_scope"][varname].Field("MangledName")));
                    }), r(empty(), (Grammar gr) =>
                    {
                        var c = gr.CodeGen;
                        var varname = c["IN"].Invoke("Match", c["m"]["myVarName"]);
                        c.If(c["m"]["myTypeName"].Field("Success").LogicalNot());
                        {
                            c.Throw(Exp.New(typeof(Exception), "To declare " + varname + " without an explicit type annotation, provide an initializer at offset " + c["o"]));
                        }
                        c.End();
                    }))));

            g.AddPattern("contextualAssignment", seq(r(seq(
                t("my"),
                p("ws"),
                //opt(seq(n("myTypeName", p("varname")), p("ws"))),
                n("myVarName", p("ContextualRef")),
                ows(),
                t("="),
                ows(),
                n("myInitializer", p(expressionName))
            ), (Grammar gr) => {
                var c = gr.CodeGen;
                var varname = c["IN"].Invoke("Match", c["m"]["myVarName"]);
                var value = c["m"]["myInitializer"].Field("Result");
				
                // get the toplevel's slot for this contextual
                c.InvokeAssign(c["s01"], c["TG_top"], "GetMangledContextual", c["current_typegen"], c["current_scope"], c["root_scope"], value.Property("Type"), varname);

                var mangled_name = c["s01"];
                
                c.If(c["current_typegen"].Invoke("GetName") == "TopLevelFrame");
                {
                    // if at runtime the program hasn't already pushed a new contextual in the current lexical scope, 
                    c.Invoke(c["cg"], "If", c["cg"].Invoke("This").Invoke("Field", "_assigned_" + c["current_scope"].Field("ID") + "_" + mangled_name).Invoke("EQ", 0));
                    {
                        c.Assign(c["OpArray"], Exp.NewArray(typeof(Operand), 1));
                        c.Assign(c["OpArray"][0], c["cg"].Invoke("This").Invoke("Field", "_ctxl_" + mangled_name));
                        // push a copy of the current contextual value onto its reversion stack,
                        c.Invoke(c["cg"], "Invoke", c["cg"].Invoke("This").Invoke("Field", "_stack_" + mangled_name), "Push", c["OpArray"]);
                        // assign the current contextual value to the toplevel frame's slot for this contextual,
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_ctxl_" + mangled_name), value);
                        // and mark that the program pushed a value to this contextual in the current lexical scope;
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_assigned_" + c["current_scope"].Field("ID") + "_" + mangled_name), 1);
                    }
                    c.Invoke(c["cg"], "Else");
                    { // otherwise, just assign to the contextual's global slot again
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_ctxl_" + mangled_name), value);
                    }
                    c.Invoke(c["cg"], "End");
                }
                c.Else();
                {
                    // if at runtime the program hasn't already pushed a new contextual in the current lexical scope, 
                    c.Invoke(c["cg"], "If", c["cg"].Invoke("This").Invoke("Field", "_assigned_" + c["current_scope"].Field("ID") + "_" + mangled_name).Invoke("EQ", 0));
                    {
                        c.Assign(c["OpArray"], Exp.NewArray(typeof(Operand), 1));
                        c.Assign(c["OpArray"][0], c["cg"].Invoke("This").Invoke("Field", "TopLevelFrame").Invoke("Field", "_ctxl_" + mangled_name));
                        // push a copy of the current contextual value onto its reversion stack,
                        c.Invoke(c["cg"], "Invoke", c["cg"].Invoke("This").Invoke("Field", "TopLevelFrame").Invoke("Field", "_stack_" + mangled_name), "Push", c["OpArray"]);
                        // assign the current contextual value to the toplevel frame's slot for this contextual,
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "TopLevelFrame").Invoke("Field", "_ctxl_" + mangled_name), value);
                        // and mark that the program pushed a value to this contextual in the current lexical scope;
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "_assigned_" + c["current_scope"].Field("ID") + "_" + mangled_name), 1);
                    }
                    c.Invoke(c["cg"], "Else");
                    { // otherwise, just assign to the contextual's global slot again
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "TopLevelFrame").Invoke("Field", "_ctxl_" + mangled_name), value);
                    }
                    c.Invoke(c["cg"], "End");
                }
                c.End();

            })));

            g.AddPattern("fieldDeclaration", r(seq(
                t("has"),
                p("ws"),
                n("fieldType", p("typeName")),
                p("ws"),
                p("ScalarRef")
            ), (Grammar gr) => {
                var c = gr.CodeGen;
                var match = c["m"];
                var fieldType = match["fieldType"].Field("Result").Cast(typeof(TypeLiteral)).Property("ConstantValue").Cast(typeof(Type));
                var varname = c["IN"].Invoke("Match", match["Scalar"]["JustTheVarName"]);
                c.Invoke(c["current_classgen"], "PublicField", fieldType, varname);
            }));

            g.AddPattern("classPreDeclaration", seq(
                t("class"),
                p("ws"),
                n("className", p("NamespaceAndClass")),
                ows(),
                r(new Lookahead(seq(t("{"), ows(), t("..."))), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    var className = c["IN"].Invoke("Match", match["className"]);
                    c.If(c["GG"].Field("ClassesByName").Invoke("ContainsKey", className));
                    {
                        c.Throw(Exp.New(typeof(InvalidOperationException), "already predeclared class with name " + className));
                    }
                    c.End();
                    c.Assign(c["current_classgen"], c["AG"].Property("Public").Invoke("Class", className));
                    c.Invoke(c["GG"].Field("ClassesByName"), "Add", className, c["current_classgen"]);
                    //c.WriteLine("Predeclared class " + className);
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                }),
                ows(),
                r(n("bodyStatements", p("classStatementListBlock")), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Result"), Exp.New(typeof(TypeLiteral), c["current_classgen"]));
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Assign(c["current_classgen"], c["TG_top"]);
                })
            ));

            g.AddPattern("classDeclaration", seq(
                t("class"),
                p("ws"),
                n("className", p("NamespaceAndClass")),
                opt(seq(p("ws"), t("is"), p("ws"), n("parentType", p("typeName")))),
                r(ows(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    var className = c["IN"].Invoke("Match", match["className"]);
                    c.If(c["GG"].Field("ClassesByName").Invoke("ContainsKey", className));
                    {
                        c.Assign(c["current_classgen"], c["GG"].Field("ClassesByName")[className]);
                    }
                    c.Else();
                    {
                        c.If(match.Invoke("HasOwnKey", "parentType"));
                        {
                            c.Assign(c["current_classgen"], c["AG"].Property("Public").Invoke("Class", className, c["GG"].Invoke("ResolveParametricType", c["AG"], c["IN"].Invoke("Match", match["parentType"]), match).Cast(typeof(TypeLiteral)).Field("t")));
                        }
                        c.Else();
                        {
                            c.Assign(c["current_classgen"], c["AG"].Property("Public").Invoke("Class", className));
                        }
                        c.End();
                        c.Invoke(c["GG"].Field("ClassesByName"), "Add", className, c["current_classgen"]);
                    }
                    c.End();
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"], c["current_scope"]));
                }),
                ows(),
                r(n("bodyStatements", p("classStatementListBlock")), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var match = c["m"];
                    c.Assign(match.Field("Result"), Exp.New(typeof(TypeLiteral), c["current_classgen"]));
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                })
            ));

            BuildRoutineDeclaration(g, "subDeclaration", seq(t("sub"),
                p("ws"),
                opt(seq(n("subName", p("varname")), ows())),
                t("("),
                n("subParams", p("paramsList")),
                t("-->"),
                ows(),
                n("returnType", p("typeName")),
                ows()), t(")"));

            g.AddPattern("methodDeclaration", seq(
                t("method"),
                p("ws"),
                opt(seq(n("subName", p("varname")), ows())),
                t("("),
                n("subParams", p("paramsList")),
                opt(seq(t("-->"),
                ows(),
                n("returnType", p("typeName")))),
                ows(),
                r(t(")"), (Grammar gr) => {
                    // descend into new subroutine-class Exec methodgen
                    var c = gr.CodeGen;
                    c.InvokeAssign(c["temp_typegen01"], c["GG"], "DescendIntoMethod", c["AG"], c["current_typegen"], c["TG_top"], c["current_scope"], c["IN"], c["m"]["subName"], c["m"]["returnType"], c["m"]["subParams"], c["current_classgen"]);
                    c.InvokeAssign(c["cg"], c["GG"], "DescendIntoSub", c["cg"], c["current_typegen"], c["temp_typegen01"]);
                    c.Assign(c["current_typegen"], c["temp_typegen01"]);
                    c.Assign(c["current_scope"], c["current_typegen"].Field("FrameScope"));
                }),
                ows(),
                r(n("bodyStatements", p("statementListBlock")), (Grammar gr) => {
                    var c = gr.CodeGen;
                    c.If(c["cg"].Invoke("IsReachable"));
                    {
                        c.Increment(c["cg"].Field("InstructionCount"));
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["cg"].Field("InstructionCount"));
                        c.Invoke(c["cg"], "Label", "inst" + c["cg"].Field("InstructionCount").Invoke("ToString"));
                    }
                    c.End();
                    c.Invoke(c["cg"], "Goto", "csmeta_return_label");
                    c.Invoke(c["cg"], "Label", "inst0");
                    // at program-runtime, we've re-entered this frame from a child invocation (or 
                    //   from a continuance of a continuation), so jump to the label (the CIL instruction)
                    //   corresponding to the saved instruction pointer (ends up JITted as a direct jump-table).
                    c.Invoke(c["cg"], "Switch", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["GG"].Invoke("GetRoutineInstructionLabels", c["cg"]));
                    //c.If(c["cg"].Invoke("IsReachable"));

                    //{
                    c.Invoke(c["cg"], "Label", "csmeta_return_label");

                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertContextualsIn", c["current_scope"], c["cg"]);

                    c.Invoke(c["cg"], "Return", c["cg"].Invoke("This").Invoke("Field", "Caller"));
                    //}
                    //c.End();
                    c.Assign(c["last_typegen"], c["current_typegen"]);
                    c.Invoke(c["cg"], "Complete");
                    c.InvokeAssign(c["current_typegen"], c["GG"], "AscendOutofSub", c["last_typegen"]);
                    c.InvokeAssign(c["cg"], c["GG"], "AscendOutofSub", 1);
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "AssignNew", c["cg"].Invoke("This").Invoke("Field", c["last_typegen"].Field("NamedSubMangledName")), c["last_typegen"], c["cg"].Invoke("This"));
                    c.Assign(c["OpArray"], Exp.NewArray(typeof(Operand), 1));
                    c.Assign(c["OpArray"][0], c["cg"].Invoke("This").Invoke("Field", c["last_typegen"].Field("NamedSubMangledName")));
                    c.Invoke(c["cg"], "InvokeStatic", c["current_classgen"], "__csmeta_init_" + c["last_typegen"].Invoke("ToString"), c["OpArray"]);
                    c.Assign(c["m"].Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["last_typegen"].Field("NamedSubMangledName")));
                })
            ));

            counter = Counter;
        }

        public static void BuildRoutineDeclaration(Grammar g, string patternName, Pattern prefix, Pattern terminator) {
            g.AddPattern(patternName, seq(
                prefix,
                r(terminator, (Grammar gr) => {
                    // descend into new subroutine-class Exec methodgen
                    var c = gr.CodeGen;
                    c.InvokeAssign(c["temp_typegen01"], c["GG"], "DescendIntoSub", c["AG"], c["current_typegen"], c["TG_top"], c["current_scope"], c["IN"], c["m"]["subName"], c["m"]["returnType"], c["m"]["subParams"]);
                    c.InvokeAssign(c["cg"], c["GG"], "DescendIntoSub", c["cg"], c["current_typegen"], c["temp_typegen01"]);
                    c.Assign(c["current_typegen"], c["temp_typegen01"]);
                    c.Assign(c["current_scope"], c["current_typegen"].Field("FrameScope"));
                }),
                ows(),
                r(n("bodyStatements", p("statementListBlock")), (Grammar gr) => {
                    var c = gr.CodeGen;
                    c.If(c["cg"].Invoke("IsReachable"));
                    {
                        c.Increment(c["cg"].Field("InstructionCount"));
                        c.Invoke(c["cg"], "Assign", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["cg"].Field("InstructionCount"));
                        c.Invoke(c["cg"], "Label", "inst" + c["cg"].Field("InstructionCount").Invoke("ToString"));
                    }
                    c.End();
                    c.Invoke(c["cg"], "Goto", "csmeta_return_label");
                    c.Invoke(c["cg"], "Label", "inst0");
                    // at program-runtime, we've re-entered this frame from a child invocation (or 
                    //   from a continuance of a continuation), so jump to the label (the CIL instruction)
                    //   corresponding to the saved instruction pointer (ends up JITted as a direct jump-table).
                    c.Invoke(c["cg"], "Switch", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["GG"].Invoke("GetRoutineInstructionLabels", c["cg"]));
                    //c.If(c["cg"].Invoke("IsReachable"));

                    //{
                    c.Invoke(c["cg"], "Label", "csmeta_return_label");

                    c.Invoke(c["TG_top"].Field("FrameGen"), "RevertContextualsIn", c["current_scope"], c["cg"]);

                    c.Invoke(c["cg"], "Return", c["cg"].Invoke("This").Invoke("Field", "Caller"));
                    //}
                    //c.End();
                    c.Assign(c["last_typegen"], c["current_typegen"]);
                    c.Invoke(c["cg"], "Complete");
                    c.InvokeAssign(c["current_typegen"], c["GG"], "AscendOutofSub", c["last_typegen"]);
                    c.InvokeAssign(c["cg"], c["GG"], "AscendOutofSub", 1);
                    c.Assign(c["current_scope"], c["current_scope"].Field("Parent").Cast(typeof(FrameScope)));
                    c.Invoke(c["cg"], "AssignNew", c["cg"].Invoke("This").Invoke("Field", c["last_typegen"].Field("NamedSubMangledName")), c["last_typegen"], c["cg"].Invoke("This"));
                    c.Assign(c["m"].Field("Result"), c["cg"].Invoke("This").Invoke("Field", c["last_typegen"].Field("NamedSubMangledName")));
                    //c.WriteLine("finished declaring a sub");
                })
            ));
        }
    }
}

