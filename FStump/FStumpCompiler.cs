using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace FStump
{
    public class FStumpCompiler
    {
        public const string ZERO = "r0"; // Always zero
        public const string G1 = "r1"; // User register
        public const string G2 = "r2"; // User register
        public const string G3 = "r3"; // User register
        public const string G4 = "r4"; // User register
        public const string LR = "r5"; // Link return
        public const string SF = "r6"; // Stack frame
        public const string PC = "r7"; // Program counter

        public const string GlobalPrefix = "GLOBAL_";
//        public const string LocalPrefix = "LOCAL_";
        public const string FuncPrefix = "FUNC_";
        public const string LabelPrefix = "LABEL_";

        
        public const int MinImm = -16;
        public const int MaxImm = 15;
        
        private StumpWriter Writer { get; set; }

        private IList<string> GlobalNames { get; set; }
        private IDictionary<string, int> FunctionNames { get; set; }
        
        
        private IList<string> LocalNames { get; set; }

        private int _uniqueNumber = 0;
        
        public void Compile(string input, string output)
        {
            try
            {
                var lexer = new FStumpLexer(new AntlrFileStream(input));
                var tokens = new CommonTokenStream(lexer);
                var parser = new FStumpParser(tokens) {BuildParseTree = true};

                Writer = new StumpWriter();
                Writer.WriteComment("Autogenerated program using fstump");

                Writer.WriteBlankLine();
                Writer.WriteComment("Setup and jump");
                Writer.WriteOrg("0");
                Writer.WriteMovImme(LR, "program_exit");
                Writer.WriteMovImme(SF, "stack_start_ptr");
                Writer.WriteLoad(SF, SF);

                Writer.WriteBranch(BranchConditions.Always, $"{FuncPrefix}main");
                Writer.WriteLabel("stack_start_ptr");
                Writer.WriteData("stack_start");

                Writer.WriteComment("Loop based noop exit");
                Writer.WriteLabel("program_exit");
                Writer.WriteNop();
                Writer.WriteBranch(BranchConditions.Always, "program_exit");

                var entry = parser.entry();

                GlobalNames = new List<string>();
                FunctionNames = new Dictionary<string, int>();

                // Pass 1: Find identifiers
                foreach (var context in entry.element())
                {
                    switch (context)
                    {
                        case FStumpParser.FunctionElementContext function:
                        {
                            var innerFunction = function.function();
                            var name = innerFunction.identifier().GetText();

                            if (FunctionNames.ContainsKey(name))
                            {
                                throw new ArgumentException($"Function {name} already exists");
                            }

                            FunctionNames.Add(name, innerFunction.functionArgs()?.functionArg()?.Length ?? 0);
                            break;
                        }
                        case FStumpParser.GlobalDecElementContext globalDec:
                            HandleGlobalDecElement(globalDec.globalDec());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(context));
                    }
                }

                // Pass 2: Generate functions
                foreach (var context in entry.element())
                {
                    switch (context)
                    {
                        case FStumpParser.FunctionElementContext function:
                            HandleFunction(function.function());
                            break;
                        case FStumpParser.GlobalDecElementContext _:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(context));
                    }
                }

                Writer.WriteBlankLine();
                Writer.WriteComment("Stack");
                Writer.WriteLabel("stack_start");
                Writer.WriteData(0);

                Console.WriteLine();
                Console.WriteLine("-----------------------------");
            }
            finally
            {
                Writer.Print();

                File.WriteAllText(output, Writer.Write());
            }
        }

        private void HandleGlobalDecElement(FStumpParser.GlobalDecContext context)
        {
            Writer.WriteBlankLine();
            switch (context)
            {
                case FStumpParser.ArrayGlobalDecContext array:
                {
                    var name = array.identifier().GetText();
                    Writer.WriteComment($"Global {name}");

                    GlobalNames.Add(name);
                    Writer.WriteLabel($"{GlobalPrefix}{name}");
                    foreach (var literalContext in array.numberLiteral())
                        Writer.WriteData(ParseNumberLiteral(literalContext));
                    break;
                }
                case FStumpParser.BlockGlobalDecContext block:
                {
                    var name = block.identifier().GetText();
                    Writer.WriteComment($"Global {name}");

                    GlobalNames.Add(name);
                    Writer.WriteLabel($"{GlobalPrefix}{name}");
                    for (var i = 0; i < ParseNumberLiteral(block.numberLiteral()); i++)
                    {
                        Writer.WriteData(0);
                    }
                    break;
                }
                case FStumpParser.LiteralGlobalDecContext literal:
                {
                    var name = literal.identifier().GetText();
                    Writer.WriteComment($"Global {name}");

                    GlobalNames.Add(name);
                    Writer.WriteLabel($"{GlobalPrefix}{name}");
                    Writer.WriteData(ParseNumberLiteral(literal.numberLiteral()));
                    break;
                }
                case FStumpParser.StringGlobalDecContext stringVal:
                {
                    var name = stringVal.identifier().GetText();
                    Writer.WriteComment($"Global {name}");

                    GlobalNames.Add(name);
                    Writer.WriteLabel($"{GlobalPrefix}{name}");

                    var content = stringVal.@string().GetText();
                    content = content.Substring(1, content.Length - 2);
                    
                    foreach (var c in content.ToCharArray())
                        Writer.WriteData(c);
                    
                    Writer.WriteData(0);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

        private void HandleFunction(FStumpParser.FunctionContext context)
        {
            Writer.WriteBlankLine();
            Writer.WriteBlankLine();

            var name = context.identifier().GetText();
            Writer.WriteComment($"--- Function {name} Start ---");

            // Process
            LocalNames = new List<string>();

            if (context.functionArgs() != null)
            {
                foreach (var arg in context.functionArgs().functionArg())
                {
                    LocalNames.Add(arg.identifier().GetText());
                }
            }

            foreach (var statement in context.statement())
            {
                switch (statement)
                {
                    case FStumpParser.LocalStatementContext local:
                    {
                        LocalNames.Add(local.identifier().GetText());
                        break;
                    }
                }
            }

            // Write method entry
            Writer.WriteLabel($"{FuncPrefix}{name}");

            var skipBlankLine = false;
            foreach (var statement in context.statement())
            {
                if (!skipBlankLine) Writer.WriteBlankLine();
                skipBlankLine = false;
                switch (statement)
                {
                    case FStumpParser.AddAssignLitStatementContext addAssignLit:
                    {
                        var val = ParseNumberLiteral(addAssignLit.val);
                        var dest = ParseRegister(addAssignLit.dest);
                        
                        if (val > MaxImm)
                        {
                            throw new NotImplementedException("Only small locals supported for now");
                        }
                        
                        Writer.WriteComment($"Adding {val} to {dest}");
                        Writer.WriteAddImme(dest, dest, val.ToString());
                        break;
                    }
                    case FStumpParser.AddAssignRegStatementContext addAssignReg:
                    {
                        var src = ParseRegister(addAssignReg.val);
                        var dest = ParseRegister(addAssignReg.dest);
                        Writer.WriteComment($"Adding {src} to {dest}");
                        Writer.WriteAddReg(dest, dest, src);
                        break;
                    }
                    case FStumpParser.AddLitStatementContext addLitStatementContext:
                        throw new NotImplementedException();
                        break;
                    case FStumpParser.AddRegStatementContext addRegStatementContext:
                        throw new NotImplementedException();
                        break;
                    case FStumpParser.AndLitStatementContext andLitStatement:
                    {
                        var srcA = ParseRegister(andLitStatement.left);
                        var val = ParseNumberLiteral(andLitStatement.right);
                        var dest = ParseRegister(andLitStatement.dest);
                        Writer.WriteComment($"Anding {srcA} with {val} to {dest}");
                        Writer.WriteAndImme(dest, srcA, val.ToString());
                        break;
                    }
                    case FStumpParser.AndRegStatementContext andRegStatement:
                    {
                        var srcA = ParseRegister(andRegStatement.left);
                        var srcB = ParseRegister(andRegStatement.right);
                        var dest = ParseRegister(andRegStatement.dest);
                        Writer.WriteComment($"Anding {srcA} with {srcB} to {dest}");
                        Writer.WriteAndReg(dest, srcA, srcB);
                        break;
                    }
                    case FStumpParser.CallStatementContext callStatement:
                        HandleCallStatement(callStatement);
                        break;
                    case FStumpParser.CmpLitStatementContext cmpLit:
                    {
                        var val = ParseNumberLiteral(cmpLit.right);
                        var dest = ParseRegister(cmpLit.left);
                        
                        if (val > MaxImm)
                        {
                            throw new NotImplementedException("Only small imm supported for now");
                        }
                        
                        Writer.WriteComment($"Comparing {dest} with {val}");
                        Writer.WriteCmpImme(dest, val.ToString());
                        break;
                    }
                    case FStumpParser.CmpRegStatementContext cmpRegStatement:
                    {
                        var left = ParseRegister(cmpRegStatement.left);
                        var right = ParseRegister(cmpRegStatement.right);
                        
                        Writer.WriteComment($"Comparing {left} with {right}");
                        Writer.WriteCmpReg(left, right);
                        break;
                    }
                    case FStumpParser.GotoCondStatementContext gotoCond:
                    {
                        Writer.WriteBranch(gotoCond.cond.GetText(), $"{LabelPrefix}{name}_{gotoCond.lab.GetText()}");
                        break;
                    }
                    case FStumpParser.GotoStatementContext gotoS:
                    {
                        Writer.WriteBranch(BranchConditions.Always, $"{LabelPrefix}{name}_{gotoS.label.GetText()}");
                        break;
                    }
                    case FStumpParser.LabelStatementContext labelStatement:
                    {
                        var labelName = labelStatement.name.GetText();
                        Writer.WriteComment($"User label {labelName}");
                        Writer.WriteLabel($"{LabelPrefix}{name}_{labelName}");
                        // TODO: Is no-op needed, hopefully label clashes shouldn't occur
                        Writer.WriteNop();
                        break;
                    }
                    case FStumpParser.LoadAddrStatementContext loadAddrStatementContext:
                        HandleLoadAddressStatement(loadAddrStatementContext);
                        break;
                    case FStumpParser.LoadRegStatementContext loadRegStatement:
                    {
                        var src = ParseRegister(loadRegStatement.src);
                        var dest = ParseRegister(loadRegStatement.dest);
                        Writer.WriteComment($"Loading address {src} to {dest}");
                        Writer.WriteLoad(dest, src);
                        break;
                    }
                    case FStumpParser.LoadStatementContext loadStatement:
                        HandleLoadStatement(loadStatement);
                        break;
                    case FStumpParser.LocalStatementContext _:
                        skipBlankLine = true;
                        break;
                    case FStumpParser.LshiftStatementContext lshiftStatement:
                        HandleLshiftStatement(lshiftStatement);
                        break;
                    case FStumpParser.NopStatementContext _:
                        Writer.WriteNop();
                        break;
                    case FStumpParser.OffsetRegLoadStatementContext offsetRegLoadStatement:
                    {
                        var baseReg = ParseRegister(offsetRegLoadStatement.@base);
                        var offReg = ParseRegister(offsetRegLoadStatement.off);
                        var destReg = ParseRegister(offsetRegLoadStatement.dest);
                        
                        Writer.WriteComment($"Loading from {baseReg} offset by {offReg} to {destReg}");
                        Writer.WriteLoad(destReg, baseReg, offReg);
                        break;
                    }
                    case FStumpParser.OffsetRegStoreStatementContext offsetRegStoreStatement:
                    {
                        var baseReg = ParseRegister(offsetRegStoreStatement.@base);
                        var offReg = ParseRegister(offsetRegStoreStatement.off);
                        var valReg = ParseRegister(offsetRegStoreStatement.val);
                        
                        Writer.WriteComment($"Storing {valReg} to {baseReg} offset by {offReg}");
                        Writer.WriteStore(valReg, baseReg, offReg);
                        break;
                    }
                    case FStumpParser.ReturnStatementContext returnStatementContext:
                        throw new NotImplementedException();
                        break;
                    case FStumpParser.SetStatementContext setStatement:
                        HandleSetStatement(setStatement);
                        break;
                    case FStumpParser.StoreRegStatementContext storeReg:
                    {
                        var src = ParseRegister(storeReg.src);
                        var dest = ParseRegister(storeReg.dest);
                        Writer.WriteComment($"Storing {src} to addr in {dest}");
                        Writer.WriteStore(src, dest);
                        break;
                    }
                    case FStumpParser.StoreStatementContext storeStatement:
                        HandleStoreStatement(storeStatement);
                        break;
                    case FStumpParser.TestLitStatementContext testLitStatementContext:
                        throw new NotImplementedException();
                        break;
                    case FStumpParser.TestRegStatementContext testRegStatementContext:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(statement));
                }
            }
            
            Writer.WriteComment("Jumping back");
            Writer.WriteMovReg(PC, LR);
            
            Writer.WriteComment($"--- Function {name} End ---");
        }

        private void HandleLoadStatement(FStumpParser.LoadStatementContext context)
        {
            var source = context.identifier().GetText();
            var dest = ParseRegister(context.dest);

            if (GlobalNames.Contains(source))
            {
                Writer.WriteComment($"Loading global {source} into {dest}");

                var label1 = GenerateLabel();
                Writer.WriteBranch(BranchConditions.Always, label1);
                
                // Place global address into a close mem location
                var label2 = GenerateLabel();
                Writer.WriteLabel(label2);
                Writer.WriteData($"{GlobalPrefix}{source}");

                // Load close mem location to get full address
                Writer.WriteLabel(label1);
                Writer.WriteLoadLabel(dest, label2);
                Writer.WriteLoad(dest, dest);
            }
            else if (LocalNames.Contains(source))
            {
                Writer.WriteComment($"Loading local {source} into {dest}");

                var index = LocalNames.IndexOf(source);

                if (index > MaxImm)
                {
                    throw new NotImplementedException("32 locals only supported for now");
                }
                
                Writer.WriteLoad(dest, SF, $"#{index.ToString()}");
            }
            else
            {
                throw new ArgumentException($"Unknown variable {source}");
            }
        }
        
        private void HandleStoreStatement(FStumpParser.StoreStatementContext context)
        {
            var dest = context.identifier().GetText();
            var source = ParseRegister(context.register());

            if (GlobalNames.Contains(dest))
            {
                Writer.WriteComment($"Storing {source} to global {dest}");

                var label1 = GenerateLabel();
                Writer.WriteBranch(BranchConditions.Always, label1);
                
                // Place global address into a close mem location
                var label2 = GenerateLabel();
                Writer.WriteLabel(label2);
                Writer.WriteData($"{GlobalPrefix}{dest}");

                // Generate space for LR
                var label3 = GenerateLabel();
                Writer.WriteLabel(label3);
                Writer.WriteData(0);

                Writer.WriteLabel(label1);
                Writer.WriteStoreLabel(LR, label3);
                
                // Load close mem location to get full address
                Writer.WriteLoadLabel(LR, label2);
                Writer.WriteStore(source, LR);
                Writer.WriteLoadLabel(LR, label3);
            }
            else if (LocalNames.Contains(source))
            {
                Writer.WriteComment($"Storing {source} into local {dest}");

                var index = LocalNames.IndexOf(dest);

                if (index > MaxImm)
                {
                    throw new NotImplementedException("32 locals only supported for now");
                }
                
                Writer.WriteStore(source, SF, $"#{index.ToString()}");
            }
            else
            {
                throw new ArgumentException($"Unknown variable {dest}");
            }
        }
        
        private void HandleLoadAddressStatement(FStumpParser.LoadAddrStatementContext context)
        {
            var source = context.identifier().GetText();
            var dest = ParseRegister(context.dest);

            if (GlobalNames.Contains(source))
            {
                Writer.WriteComment($"Loading address of global {source} into {dest}");

                var label1 = GenerateLabel();
                Writer.WriteBranch(BranchConditions.Always, label1);
                
                // Place global address into a close mem location
                var label2 = GenerateLabel();
                Writer.WriteLabel(label2);
                Writer.WriteData($"{GlobalPrefix}{source}");

                // Load close mem location to get full address
                Writer.WriteLabel(label1);
                Writer.WriteLoadLabel(dest, label2);
            }
            else if (LocalNames.Contains(source))
            {
                Writer.WriteComment($"Loading address of local {source} into {dest}");

                var index = LocalNames.IndexOf(source);

                if (index > MaxImm)
                {
                    throw new NotImplementedException("32 locals only supported for now");
                }
                
                Writer.WriteAddImme(dest, SF, index.ToString());
            }
            else
            {
                throw new ArgumentException($"Unknown variable {source}");
            }
        }

        private void HandleLshiftStatement(FStumpParser.LshiftStatementContext context)
        {
            var shift = ParseNumberLiteral(context.right);
            var dest = ParseRegister(context.dest);
            var src = ParseRegister(context.left);

            Writer.WriteComment($"Left shifting {src} by {shift} into {dest}");
            
            if (shift == 0)
            {
                Writer.WriteMovReg(dest, src);
                return;
            }

            Writer.WriteAddReg(dest, src, src);
            for (var i = 0; i < shift - 1; i++)
            {
                Writer.WriteAddReg(dest, dest, dest);
            }
        }

        private void HandleSetStatement(FStumpParser.SetStatementContext context)
        {
            var value = ParseNumberLiteral(context.val);
            var dest = ParseRegister(context.register());

            Writer.WriteComment($"Setting {dest} to {value}");
            if (value > MaxImm)
            {
                var label1 = GenerateLabel();
                Writer.WriteBranch(BranchConditions.Always, label1);

                // Place value into a close mem location
                var label2 = GenerateLabel();
                Writer.WriteLabel(label2);
                Writer.WriteData(value);

                // Load close mem location to get full value
                Writer.WriteLabel(label1);
                Writer.WriteLoadLabel(dest, label2);
                return;
            }
            
            Writer.WriteMovImme(dest, value.ToString());
        }

        private void HandleCallStatement(FStumpParser.CallStatementContext context)
        {
            var name = context.identifier().GetText();
            
            Writer.WriteComment($"Calling function {name}");

            if (!FunctionNames.TryGetValue(name, out var argCount))
            {
                throw new ArgumentException($"Invalid call to {name}: Unknown function");
            }

            var localCount = LocalNames.Count;
            
            if (localCount > MaxImm)
            {
                throw new NotImplementedException("Only calling from low local count methods support for now");
            }

            var output = context.register() != null ? ParseRegister(context.register()) : ""; 
            
            // Save registers
            Writer.WriteComment("Saving registers");
            Writer.WriteAddImme(SF, SF, localCount.ToString());
            Writer.WriteStore(G1, SF, "#0");
            Writer.WriteStore(G2, SF, "#1");
            Writer.WriteStore(G3, SF, "#2");
            Writer.WriteStore(G4, SF, "#3");
            Writer.WriteStore(LR, SF, "#4");
            Writer.WriteAddImme(SF, SF, "5");

            var actualArgCount = 0;
            if (context.callArgs() != null)
            {
                Writer.WriteComment("Copying arguments");
                var index = 0;
                foreach (var arg in context.callArgs().callArg())
                {
                    switch (arg)
                    {
                        case FStumpParser.IdenCallArgContext idenCall:
                        {
                            var iden = idenCall.identifier().GetText();
                            if (GlobalNames.Contains(iden))
                            {
                                var label1 = GenerateLabel();
                                Writer.WriteBranch(BranchConditions.Always, label1);

                                // Place global address into a close mem location
                                var label2 = GenerateLabel();
                                Writer.WriteLabel(label2);
                                Writer.WriteData($"{GlobalPrefix}{iden}");

                                // Load close mem location to get full address
                                Writer.WriteLabel(label1);
                                Writer.WriteLoadLabel(G1, label2);
                                Writer.WriteLoad(G1, G1);
                            }
                            else if (LocalNames.Contains(iden))
                            {
                                var localIndex = LocalNames.IndexOf(iden);

                                if (index > MaxImm)
                                {
                                    throw new NotImplementedException("32 locals only supported for now");
                                }

                                Writer.WriteAddImme(SF, SF, "-5");
                                Writer.WriteLoad(G1, SF, $"#{localIndex.ToString()}");
                                Writer.WriteAddImme(SF, SF, "5");
                            }
                            else
                            {
                                throw new ArgumentException($"Unknown identifier {iden}");
                            }
                            break;
                        }
                        case FStumpParser.LitCallArgContext litArg:
                        {
                            var value = ParseNumberLiteral(litArg.numberLiteral());
                            Writer.WriteMovImme(G1, value.ToString());
                            break;
                        }
                        case FStumpParser.RegCallArgContext regCall:
                        {
                            var regName = ParseRegister(regCall.register());
                            var offset = -5 + GetRegisterFramePos(regName);
                            Writer.WriteLoad(G1, SF, $"#{offset}");
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(arg));
                    }
                    
                    Writer.WriteStore(G1, SF, $"#{index}");
                    index++;
                }

                actualArgCount = index;
            }

            if (argCount != actualArgCount)
            {
                throw new ArgumentException($"Invalid call to {name}: Expected {argCount} args");
            }

            Writer.WriteComment("Jumping");
            var jmpLabel = GenerateLabel();
            var retLabel = GenerateLabel();
            var retLabelAddr = GenerateLabel();
            
            Writer.WriteLoadLabel(LR, retLabelAddr);
            
            Writer.WriteLoadLabel(G1, jmpLabel);
//            Writer.WriteLoad(G1, G1);
            Writer.WriteMovReg(PC, G1);


            // Place global address into a close mem location
            Writer.WriteLabel(jmpLabel);
            Writer.WriteData($"{FuncPrefix}{name}");
            
            Writer.WriteLabel(retLabelAddr);
            Writer.WriteData($"{retLabel}");

            Writer.WriteComment("Restoring registers");
            Writer.WriteLabel(retLabel);

            Writer.WriteAddImme(SF, SF, "-5");
            if(output != G1) Writer.WriteLoad(G1, SF, "#0");
            if(output != G2) Writer.WriteLoad(G2, SF, "#1");
            if(output != G3) Writer.WriteLoad(G3, SF, "#2");
            if(output != G4) Writer.WriteLoad(G4, SF, "#3");
            if(output != LR) Writer.WriteLoad(LR, SF, "#4");

            Writer.WriteAddImme(SF, SF, $"-{localCount.ToString()}");
        }

        private int GetRegisterFramePos(string register)
        {
            switch (register)
            {
                case G1:
                    return 0;
                case G2:
                    return 1;
                case G3:
                    return 2;
                case G4:
                    return 3;
                case LR:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(register));
            }
        }

        private string GenerateLabel()
        {
            return $"autogen_{_uniqueNumber++}";
        }

        private int ParseNumberLiteral(FStumpParser.NumberLiteralContext context)
        {
            switch (context)
            {
                case FStumpParser.BinaryNumberLiteralContext binaryNumber:
                    return Convert.ToInt32(binaryNumber.GetText().Substring(2), 2);
                case FStumpParser.CharNumberLiteralContext charNumber:
                {
                    var content = charNumber.GetText();
                    
                    if (content.Length != 3)
                        throw new ArgumentException($"Unexpected char val {content}"); 
                    
                    return content[1];
                }
                case FStumpParser.DecimalNumberLiteralContext decimalNumber:
                    return Convert.ToInt32(decimalNumber.GetText());
                case FStumpParser.HexNumberLiteralContext hexNumber:
                    return Convert.ToInt32(hexNumber.GetText(), 16);
                case FStumpParser.OctNumberLiteralContext octNumberLiteralContext:
                    throw new NotImplementedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

        private string ParseRegister(FStumpParser.RegisterContext context)
        {
            switch (context)
            {
                case FStumpParser.LrRegisterContext _:
                    return LR;
                case FStumpParser.PcRegisterContext _:
                    return PC;
                case FStumpParser.R1RegisterContext _:
                    return G1;
                case FStumpParser.R2RegisterContext _:
                    return G2;
                case FStumpParser.R3RegisterContext _:
                    return G3;
                case FStumpParser.R4RegisterContext _:
                    return G4;
                case FStumpParser.SfRegisterContext _:
                    return SF;
                case FStumpParser.ZeroRegisterContext _:
                    return ZERO;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

//        private void HandleEntry(FStumpParser.EntryContext context)
//        {
//            foreach (var functionContext in context.function())
//            {
//                HandleFunction(functionContext);
//            }
//        }
//
//        private void HandleFunction(FStumpParser.FunctionContext context)
//        {
//            var functionName = context.identifier().GetText();
//
//            Writer.WriteBlankLine();
//            Writer.WriteComment($"Function start {functionName}");
//
//            var oldWriter = Writer;
//            
//            Writer = new StumpWriter();
//            StackFrame = new StackFrame();
//
//            HandleBlock(context.block());
//
//            StackFrame.WriteFrame(oldWriter);
//
//            oldWriter.WriteComment("Function body");
//            oldWriter.WriteWriter(Writer);
//            Writer = oldWriter;
//            
//            Writer.WriteComment($"Function end {functionName}");
//            Writer.WriteBlankLine();
//        }
    }
}