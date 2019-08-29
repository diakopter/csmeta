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
        public static Grammar BuildPerlesque(string imageName, bool saveStage1AssemblyToDisk, bool saveStage2AssemblyToDisk)
        {
            var g = new Grammar(imageName);
            var Counter = 0;

            var expressionName = "EXPR";
            var termName = "Term";
            var termishName = "Termish";

            g.AddPattern("toplevel", seq(
                r(new Empty(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;
                    var unused = c["AG", typeof(AssemblyGen)];
                    //unused = c["AG2", typeof(AssemblyGen)];
                    unused = c["dyn_asmbly", typeof(Assembly)];
                    //unused = c["TG2", typeof(TypeGen)];
                    unused = c["TG", typeof(TypeGen)];
                    unused = c["TG_top", typeof(TypeGen)];
                    unused = c["MG", typeof(MethodGen)];
                    unused = c["TLMG", typeof(MethodGen)];
                    unused = c["TLBG", typeof(MethodGen)];
                    unused = c["temp_methodgen01", typeof(MethodGen)];
                    unused = c["cg_2", typeof(CodeGen)];
                    unused = c["cg", typeof(CodeGen)];
                    unused = c["null_value", typeof(Operand)];
                    unused = c["OpArray", typeof(Operand[])];
                    unused = c["OpArray2", typeof(Operand[])];
                    unused = c["VC", typeof(int)];
                    unused = c["assertion", typeof(bool)];
                    unused = c["assertion_counter", typeof(int)];
                    unused = c["ANew", typeof(Assembly)];
                    unused = c["current_scope", typeof(FrameScope)];
                    unused = c["last_scope", typeof(FrameScope)];
                    unused = c["root_scope", typeof(FrameScope)];
                    unused = c["current_typegen", typeof(TypeGen)];
                    unused = c["current_classgen", typeof(TypeGen)];
                    unused = c["temp_typegen01", typeof(TypeGen)];
                    unused = c["temp_typegen02", typeof(TypeGen)];
                    unused = c["temp_type", typeof(Type)];
                    unused = c["last_typegen", typeof(TypeGen)];
                    unused = c["assembly_name", typeof(string)];
                    unused = c["temp_fieldgen01", typeof(FieldGen)];
                    unused = c["if_counter", typeof(uint)];
                    unused = c["if_current", typeof(uint)];
                    unused = c["if_stack", typeof(Stack<uint>)];
                    unused = c["loop_counter", typeof(uint)];
                    unused = c["loop_current", typeof(uint)];
                    unused = c["loop_stack", typeof(Stack<uint>)];
                    unused = c["temp_field03", typeof(Field)];
                    unused = c["temp_subname", typeof(string)];
                    unused = c["s01", typeof(string)];
                    unused = c["last_line", typeof(int)];
                    unused = c["last_position", typeof(int)];
                    unused = c["foreach_string", typeof(string)];

                    c.Assign(c["loop_current"], 0);
                    c.Assign(c["loop_counter"], 0);
                    c.Assign(c["loop_stack"], Exp.New(typeof(Stack<uint>)));
                    c.Assign(c["if_counter"], 0);
                    c.Assign(c["if_current"], 0);
                    c.Assign(c["if_stack"], Exp.New(typeof(Stack<uint>)));
                    c.Assign(c["last_line"], 1);
                    c.Assign(c["last_position"], 0);
                    c.Assign(c["assertion_counter"], 0);
                    c.Assign(c["VC"], 1);
                    c.Assign(c["assembly_name"], c["GG"].Invoke("GetNextAssemblyName"));
                    //c.Assign(c["AG2"], c["GG"].Invoke("GetAssemblyGen", c["assembly_name"] + "_throwaway"));
                    //c.Assign(c["TG2"], c["GG"].Invoke("GetTypeGen", c["AG2"]));
                    c.Assign(c["AG"], c["GG"].Invoke("GetAssemblyGen", c["assembly_name"]));
                    c.Assign(c["TG"], c["GG"].Invoke("GetTypeGen", c["AG"]));
                    c.Assign(c["MG"], c["GG"].Invoke("GetEntryPointGen", c["TG"]));
                    c.Assign(c["cg_2"], c["MG"]);
                    c.Assign(c["TG_top"], c["GG"].Invoke("GetToplevelTypeGen", c["AG"]));
                    c.Assign(c["current_typegen"], c["TG_top"]);
                    c.Assign(c["TLMG"], c["GG"].Invoke("GetExecGen", c["TG_top"]));

                    c.Assign(c["type1"], c["GG"].Field("FrameBaseGen"));

                    // empty implementation of Bind for the toplevel
                    //c.Assign(c["TLBG"], c["GG"].Invoke("GetBindGen", c["TG_top"], c["type1"]));

                    //c.Assign(c["type1"], Exp.New(typeof(Operand[]), 1));
                    //c.Assign(c["OpArray"][0], c["TG_top"].Invoke("GetTypeObject"));

                    c.Assign(c["cg"], c["MG"]);
                    c.Assign(c["cg"]["operand01"], c["cg"]["evaluator", c["TG_top"].Invoke("GetTypeObject")]);
                    c.InvokeAssign(c["cg"]["evaluator"], typeof(Exp), "New", c["TG_top"].Invoke("GetTypeObject"));
                    c.Invoke(c["cg"], "Invoke", c["cg"]["evaluator"], "Run");


                    c.Assign(c["cg"], c["TLMG"]);
                    c.Assign(c["cg"]["operand01"], c["cg"]["next_frame", c["GG"].Field("FrameBaseGen")]);
                    // create the base FrameScope
                    c.Assign(c["current_scope"], Exp.New(typeof(FrameScope), c["current_typegen"]));
                    c.Assign(c["root_scope"], c["current_scope"]);
                    c.Invoke(c["cg"], "Goto", "csmeta_prologue");
                    c.Invoke(c["cg"], "Label", "csmeta_actual_start");
                    //c.Try();
                }),
                p("optStatementList"),
                r(end(), (Grammar gr) =>
                {
                    var c = gr.CodeGen;

                    c.Invoke(c["cg"], "Goto", "inst2");
                    c.Invoke(c["cg"], "Label", "csmeta_prologue");
                    // initialization code for the toplevelframe
                    //   (initialize stacks of contextuals)
                    c.Invoke(c["TG_top"], "EmitPrologueInitializers", c["cg"]);
                    c.Invoke(c["cg"], "Goto", "csmeta_actual_start");
                    c.Invoke(c["cg"], "Label", "inst0");
                    c.Invoke(c["cg"], "Switch", c["cg"].Invoke("This").Invoke("Field", "Instruction"), c["GG"].Invoke("GetRoutineInstructionLabels", c["cg"]));
                    c.Invoke(c["cg"], "Label", "inst2");
                    c.Invoke(c["cg"], "Label", "csmeta_return_label");
                    c.Invoke(c["cg"], "Return", c["null_value"]);
                    //c.CatchAll();
                    //c.WriteLine("parse failure; got to line " + c["last_line"]);
                    //c.End();
                    c.Invoke(c["GG"], "AscendOutofSub", c["TG_top"]);
                    if (saveStage2AssemblyToDisk)
                    {
                        c.Invoke(c["GG"], "SaveAndInvokeAssembly", c["AG"], c["assembly_name"]);
                    }
                    else
                    {
                        c.InvokeAssign(c["dyn_asmbly"], c["AG"], "GetAssembly");
                        c.Invoke(c["GG"], "InvokeEntryPoint", c["dyn_asmbly"]);
                    }
                    c.Goto(g.DoneLabel);
                    //c.WriteLine("compiled to assembly " + c["assembly_name"] + ".dll");
                    //c.Invoke(c["GG"], "RunAssembly", c["assembly_name"]);
                })
            ));
            BuildControlFlow(g, ref Counter, expressionName, termName, termishName);
            BuildTerminals(g, ref Counter, expressionName, termName, termishName);
            BuildOperators(g, ref Counter, expressionName, termName, termishName);
            BuildExpressions(g, ref Counter, expressionName, termName, termishName);
            BuildDeclarations(g, ref Counter, expressionName, termName, termishName);
            return g;
        }

        public static Pattern lb(string literal)
        {
            return new Lookbehind(literal);
        }

        public static Pattern ows()
        {
            return opt(p("ws"));
        }

        public static Pattern r(Pattern p, Action<Grammar> g)
        {
            return new ReduceAction(p, g);
        }

        public static Pattern n(string name, Pattern p)
        {
            return new NamedGroup(name, p);
        }

        public static Pattern d(string msg)
        {
            return r(empty(), (Grammar g) =>
            {
                var c = g.CodeGen;
                c.WriteLine(msg + " at " + c["o"]);
            });
        }

        public static Pattern empty()
        {
            return new Empty();
        }

        public static Pattern end()
        {
            return new End();
        }

        public static Pattern alt(params Pattern[] pats)
        {
            return Grammar.Alt(pats);
        }

        public static Pattern seq(params Pattern[] pats)
        {
            return Grammar.All(pats);
        }

        public static Pattern t(string literal)
        {
            return new Literal(literal);
        }

        public static Pattern t(char literal)
        {
            return new Literal(literal.ToString());
        }

        public static Pattern p(string name)
        {
            return new PatternRef(name);
        }

        public static Pattern opt(Pattern p)
        {
            return Grammar.Alt(p, new Empty());
        }

        public static Pattern NOffset(string msg)
        {
            return new Not(Offset(msg));
        }

        public static Pattern rep(Pattern p, int l, int h)
        {
            return new Repetition(p, l, h);
        }

        public static Pattern finished(Pattern p)
        {
            return new Finished(p);
        }

        public static Pattern final(Pattern p)
        {
            return new Final(p);
        }

        public static Pattern Offset(string msg)
        {
            return new ReduceAction(new Empty(), (Grammar gr) =>
            {
                var c = gr.CodeGen;
                c.WriteLine(msg + " at offset " + c["o"]);
            });
        }

        public static void Test33(ref int i1, ref string s1)
        {
            i1 = 44;
            s1 = "hihi";
        }
    }
    }

