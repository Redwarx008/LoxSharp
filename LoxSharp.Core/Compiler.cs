using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    internal class Compiler
    {
        private const int MAX_CONSTANTS = 65536;
        private const int MAX_VARIABLE_NAME = 64;
        private const int MAX_MODULE_VARS = 65536;
        private const int MAX_LOCAL_VARS = 65536;
        private const int MAX_PARAMETERS = 16;
        private delegate void ParseFunc(CompileState state, bool canAssign);
        private class Parser
        {
            public VM VM { get; private set; }
            public Module Module { get; private set; }
            public ref Token Previous => ref _previous;
            public ref Token Current => ref _current;
            public bool HasError { get; set; } = false;

            private Token _previous;

            private Token _current;

            private Scanner _scanner;
            public Parser(VM vm, Module module, string source)
            {
                VM = vm;
                Module = module;
                _scanner = new Scanner(source);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Advance()
            {
                Previous = Current;
                Current = _scanner.ScanToken();
            }

            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Consume(TokenType expected, string message)
            {
                Advance();
                if (Previous.Type != expected)
                {
                    Error(message);

                    // If the next token is the one we want, assume the current one is just a
                    // spurious error and discard it to minimize the number of cascaded errors.
                    if (Current.Type == expected) Advance();    
                }
            }

            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Check(TokenType type)
            {
                return Current.Type == type;
            }

            [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Match(TokenType type)
            {
                if (!Check(type))
                {
                    return false;
                }
                Advance();
                return true;
            }

            private void Error(string message)
            {
                HasError = true;

                if (VM.Config.PrintErrorFn == null)
                {
                    return;
                }

                string errorPosition;
                if (Previous.Type == TokenType.EOF)
                {
                    errorPosition = "Error at the end ";
                }
                else
                {
                    errorPosition = $"Error at '{Previous.Lexeme}' ";
                }

                string moduleName = Module.Name ?? "<unknown>";

                VM.Config.PrintErrorFn.Invoke(ErrorType.CompileError,
                    moduleName, Previous.Line, errorPosition + message);
            }
        }

        private enum FunctionType
        {
            ModuleFunction,
            ClassMethod,
            Initialized,
            Moudle  // top-level
        }

        private enum Precedence
        {
            None,
            Assignment, // =
            Or,         // or
            And,        // and
            Equality,   // == !=
            Comparison, // < > <= >=
            Term,       // + -
            Factor,     // * /
            Unary,      // ! -
            Call,       // . ()
            Primary,
        }

        private class ParseRule
        {
            public ParseFunc? Prefix { get; private set; } = null;
            public ParseFunc? Infix { get; private set; } = null;
            public Precedence Precedence { get; private set; } = Precedence.None;

            public ParseRule(ParseFunc? prefix, ParseFunc? infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precedence;
            }
        }

        private class LocalVariabal
        {
            public string Name { get; private set; }
            public int Depth { get; set; }

            public LocalVariabal(string name, int depth)
            {
                Name = name;
                Depth = depth;
            }
        }

        private class CompileState
        {
            internal class LoopState
            {
                public int LoopStart { get; set; } = 0;
                public int ScopeDepth { get; set; } = 0;
                public List<int> BreakJumpStarts { get; set; } = new();
            }
            public List<LocalVariabal> LocalVars { get; private set; }
            public ClassCompileInfo? EnclosingClass { get; set; }
            public Stack<LoopState> LoopStates { get; private set; }
            public int ScopeDepth { get; set; } = 0;
            public FunctionType FunctionType { get; private set; }
            public Function Function { get; private set; }
            public Parser Parser { get; private set; }
            public CompileState? EnclosingCompile { get; set; }
            public CompileState(Parser parser, CompileState? enclosingCompile, FunctionType functionType)
            {
                LocalVars = new List<LocalVariabal>(16);
                LoopStates = new Stack<LoopState>();
                Function = new Function(parser.Module);
                FunctionType = functionType;
                Parser = parser;
                EnclosingCompile = enclosingCompile;
                // In the VM, stack slot 0 stores the calling function. 
                if (functionType == FunctionType.ClassMethod || functionType == FunctionType.Initialized)
                {
                    LocalVars.Add(new LocalVariabal("this", 0));
                }
                else
                {
                    LocalVars.Add(new LocalVariabal("PlaceHolder", 0));
                }

                if (functionType == FunctionType.Moudle)
                {
                    Function.Name = parser.Module.Name; 
                }
                else
                {
                    Function.Name = parser.Previous.Lexeme;
                }
            }
        }
        internal class ClassCompileInfo
        {

        }

        private static readonly ParseRule[] _rules;
        static Compiler()
        {
            _rules = new ParseRule[Enum.GetValues(typeof(TokenType)).Length];

            _rules[(int)TokenType.LEFT_PAREN] = new ParseRule(Grouping, Call, Precedence.Call);
            _rules[(int)TokenType.RIGHT_PAREN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACKET] = new ParseRule(ArrayCreate, ArrayOrMapIndex, Precedence.Call);
            _rules[(int)TokenType.RIGHT_BRACKET] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.COMMA] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.DOT] = new ParseRule(null, Dot, Precedence.Call);
            _rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            _rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            _rules[(int)TokenType.SEMICOLON] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.PERCENT] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            _rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EQUAL_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
            _rules[(int)TokenType.STRING] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.NUMBER] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.AND] = new ParseRule(null, And, Precedence.And);
            _rules[(int)TokenType.CLASS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.ELSE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.FOR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FUN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.IF] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.NULL] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.OR] = new ParseRule(null, Or, Precedence.Or);
            _rules[(int)TokenType.RETURN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SUPER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.THIS] = new ParseRule(This, null, Precedence.None);
            _rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.VAR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.WHILE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EOF] = new ParseRule(null, null, Precedence.None);
        }

        public static Function? Compile(VM vm, string moduleName, string source)
        {
            // See if the module has already been loaded.
            Module? module = GetModule(vm, moduleName);
            if (module == null)
            {
                module = new Module(moduleName);
                // Store it in the VM's module registry so we don't load the same module
                // multiple times.
                vm.LoadedModules[moduleName] = module;

                // Implicitly import the core module.
                Module coreModule = GetModule(vm, string.Empty)!;
                foreach(var varIndexs in coreModule.VariableIndexes)
                {
                    DefineModuleVariable(module, varIndexs.Key, coreModule.Variables[varIndexs.Value]); 
                }
            }

            Parser parser = new Parser(vm, module, source);

            CompileState compile = new(parser, null, FunctionType.Moudle);

            parser.Advance();
            while (!parser.Match(TokenType.EOF))
            {
                Definition(compile);
            }

            return EndCompile(compile);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BeginScope(CompileState compile)
        {
            ++compile.ScopeDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EndScope(CompileState compile)
        {

            --compile.ScopeDepth;

            while (compile.LocalVars.Count > 0 &&
                compile.LocalVars[^1].Depth >
                compile.ScopeDepth)
            {
                EmitOp(compile, OpCode.POP);
                compile.LocalVars.RemoveAt(compile.LocalVars.Count - 1);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void BeginCompilerState(FunctionType functionType)
        //{
        //    CompileState compilerState = new(FunctionType.Script);
        //    if (functionType != FunctionType.Script)
        //    {
        //        compilerState.Function.Name = _previousToken.Lexeme;
        //    }
        //    else
        //    {
        //        compilerState.Function.Name = "Main";
        //    }

        //    _functionStates.Push(compilerState);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Function? EndCompile(CompileState compile)
        {
            if (compile.Parser.HasError) return null;
#if DEBUG
            Disassembler.DisassembleFunction(compile.Function);
#endif
            EmitReturn(compile);
            return compile.Function;
        }

        #region utility method

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitBytes(CompileState compile, params byte[] b)
        {
            for (int i = 0; i < b.Length; ++i)
            {
                compile.Function.Chunk.WriteByte(b[i], compile.Parser.Previous.Line);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitByte(CompileState compile, byte b)
        {
            compile.Function.Chunk.WriteByte(b, compile.Parser.Previous.Line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitOp(CompileState compile, OpCode instruction)
        {
            EmitByte(compile, (byte)instruction);  
        }
        /// <summary>
        /// Emits one 16-bit argument, which will be written big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitShort(CompileState compile, int arg)
        {
            byte high = (byte)((arg >> 8) & 0xff);
            byte low = (byte)(arg & 0xff);  
            EmitBytes(compile, high, low);
        }

        /// <summary>
        /// Emits one bytecode instruction followed by a 16-bit argument, 
        /// which will bewritten big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitOpWithShortArg(CompileState compile, OpCode instruction, int arg)
        {
            EmitOp(compile, instruction);
            EmitShort(compile, arg);    
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitOpWithArg(CompileState compile, OpCode instruction, byte arg)
        {
            EmitBytes(compile, (byte)instruction, arg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EmitJump(CompileState compile, OpCode instruction)
        {
            EmitBytes(compile, (byte)instruction, 0xff, 0xff);
            return compile.Function.Chunk.Instructions.Count - 1 - 2 + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PatchLoopBreakJumps(CompileState compile, CompileState.LoopState loopState)
        {
            for (int i = 0; i < loopState.BreakJumpStarts.Count; ++i)
            {
                PatchJump(compile, loopState.BreakJumpStarts[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitLoop(CompileState compile, int loopStart)
        {
            EmitOp(compile, OpCode.LOOP);

            int offset = compile.Function.Chunk.Instructions.Count - loopStart + 2;
            if (offset > ushort.MaxValue)
            {
                throw new CompilerException(compile.Parser.Previous, "Loop body too large.");
            }

            EmitShort(compile, offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitReturn(CompileState compile)
        {
            if (compile.FunctionType == FunctionType.Initialized)
            {
                EmitOpWithShortArg(compile, OpCode.GET_LOCAL, 0);
            }
            else
            {
                // Implicitly return null. 
                EmitOp(compile, OpCode.NULL);
            }

            EmitOp(compile, OpCode.RETURN);
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static void EmitConstant(CompileState compile, Value val)
        {
            int index = AddConstant(compile, val);
            if (index > Byte.MaxValue)
            {
                EmitOpWithShortArg(compile, OpCode.CONSTANT_16, index);
            }
            else
            {
                EmitOpWithArg(compile, OpCode.CONSTANT_8, (byte)index);
            }
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private static int AddConstant(CompileState compile, Value val)
        {
            if (compile.Parser.HasError) return -1;
            int index = compile.Function.Chunk.AddConstant(val);

            if (index == MAX_CONSTANTS)
            {
                Error(compile, $"A function can only contain {MAX_CONSTANTS} unique constants.");
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ParseRule GetRule(TokenType type)
        {
            return _rules[(int)type];
        }

        /// <summary>
        /// Looks up the previously loaded module with [moduleName].
        /// </summary>
        /// <returns>Returns <see langword="null"/> if no module with that name has been loaded.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Module? GetModule(VM vm, string moduleName)
        {
            vm.LoadedModules.TryGetValue(moduleName, out Module? module);
            return module;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Error(CompileState compile, string message)
        {
            Parser parser = compile.Parser;
            parser.HasError = true;

            if (parser.VM.Config.PrintErrorFn == null)
            {
                return;
            }

            string errorPosition;
            if (parser.Previous.Type == TokenType.EOF)
            {
                errorPosition = "Error at the end ";
            }
            else
            {
                errorPosition = $"Error at '{parser.Previous.Lexeme}' ";
            }

            string moduleName = parser.Module.Name ?? "<unknown>";

            parser.VM.Config.PrintErrorFn.Invoke(ErrorType.CompileError, 
                moduleName, parser.Previous.Line, errorPosition + message);
        }

        #endregion

        #region Parse literal method

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Literal(CompileState compile, bool canAssign)
        {
            switch (compile.Parser.Previous.Type)
            {
                case TokenType.FALSE:
                    EmitOp(compile, OpCode.FALSE);
                    break;
                case TokenType.TRUE:
                    EmitOp(compile, OpCode.TRUE);
                    break;
                case TokenType.NULL:
                    EmitOp(compile, OpCode.NULL);
                    break;
                case TokenType.NUMBER:
                    double value = Double.Parse(compile.Parser.Previous.Lexeme);
                    EmitConstant(compile, new Value(value));
                    break;
                case TokenType.STRING:
                    EmitConstant(compile, new Value(compile.Parser.Previous.Lexeme));
                    break;
                default: return; // Unreachable.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Variable(CompileState compile, bool canAssign)
        {
            NamedVariable(compile, compile.Parser.Previous.Lexeme, canAssign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void NamedVariable(CompileState compile, string variableName, bool canAssign)
        {
            OpCode getOp, setOp;

            int index = ResolveLocalVar(compile, variableName);
            if (index != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                index = ResolveModuleVar(compile.Parser.Module, variableName);
                getOp = OpCode.GET_MODULE_VAR;
                setOp = OpCode.SET_MODULE_VAR;
            }

            if (index == -1)
            {
                Error(compile, $"The name '{variableName}' does not exist in the current context.");
            }

            if (canAssign && compile.Parser.Match(TokenType.EQUAL))
            {
                Expression(compile);
                EmitOpWithShortArg(compile, setOp, index);
            }
            else
            {
                EmitOpWithShortArg(compile, getOp, index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Unary(CompileState compile, bool canAssign)
        {
            TokenType operatorType = compile.Parser.Previous.Type;

            // Compile the operand.
            ParsePrecedence(compile, Precedence.Unary);

            switch (operatorType)
            {
                case TokenType.BANG:
                    EmitOp(compile, OpCode.NOT);
                    break;
                case TokenType.MINUS:
                    EmitOp(compile, OpCode.NEGATE);
                    break;
                default:
                    return;// Unreachable.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Binary(CompileState compile, bool canAssign)
        {
            TokenType operatorType = compile.Parser.Previous.Type;
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence(compile, rule.Precedence + 1);

            switch (operatorType)
            {
                case TokenType.BANG_EQUAL:
                    EmitBytes(compile, (byte)OpCode.EQUAL, (byte)OpCode.NOT);
                    break;
                case TokenType.EQUAL_EQUAL:
                    EmitOp(compile, OpCode.EQUAL);
                    break;
                case TokenType.GREATER:
                    EmitOp(compile, OpCode.GREATER);
                    break;
                case TokenType.GREATER_EQUAL:
                    EmitBytes(compile, (byte)OpCode.LESS, (byte)OpCode.NOT);
                    break;
                case TokenType.LESS:
                    EmitOp(compile, OpCode.LESS);
                    break;
                case TokenType.LESS_EQUAL:
                    EmitBytes(compile, (byte)OpCode.GREATER, (byte)OpCode.NOT);
                    break;
                case TokenType.PLUS:
                    EmitOp(compile, OpCode.ADD);
                    break;
                case TokenType.MINUS:
                    EmitOp(compile, OpCode.SUBTRACT);
                    break;
                case TokenType.STAR:
                    EmitOp(compile, OpCode.MULTIPLY);
                    break;
                case TokenType.SLASH:
                    EmitOp(compile, OpCode.DIVIDE);
                    break;
                case TokenType.PERCENT:
                    EmitOp(compile, OpCode.MOD);
                    break;
                default: return; // Unreachable.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Call(CompileState compile, bool canAssgin)
        {
            byte argCount = ParseCallArgumentList(compile);
            EmitOpWithArg(compile, OpCode.CALL, argCount);
        }

        private static void Dot(CompileState compile, bool canAssign)
        {
            Parser parser = compile.Parser;
            parser.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            int nameConstIndex = AddConstant(compile, new Value(parser.Previous.Lexeme));

            if (canAssign && parser.Match(TokenType.EQUAL))
            {
                Expression(compile);
                EmitOpWithShortArg(compile, OpCode.SET_PROPERTY, nameConstIndex);
            }
            else if (parser.Match(TokenType.LEFT_PAREN)) // direct invoke
            {
                byte argCount = ParseCallArgumentList(compile);
                EmitOpWithShortArg(compile, OpCode.INVOKE, nameConstIndex);
                EmitByte(compile, argCount);
            }
            else
            {
                EmitOpWithShortArg(compile, OpCode.GET_PROPERTY, nameConstIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Grouping(CompileState compile, bool canAssign)
        {
            Expression(compile);
            compile.Parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void And(CompileState compile, bool canAssign)
        {
            int endJump = EmitJump(compile, OpCode.JUMP_IF_FALSE);

            EmitOp(compile, OpCode.POP);
            ParsePrecedence(compile, Precedence.And);

            PatchJump(compile, endJump);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Or(CompileState compile, bool canAssign)
        {
            int elseJump = EmitJump(compile, OpCode.JUMP_IF_FALSE);
            int endJump = EmitJump(compile, OpCode.JUMP);

            PatchJump(compile, elseJump);
            EmitOp(compile, OpCode.POP);

            ParsePrecedence(compile, Precedence.Or);
            PatchJump(compile, endJump);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void This(CompileState compile, bool canAssign)
        {
            if (GetEnclosingClass(compile) == null)
            {
                Error(compile, "Can't use 'this' outside of a class.");
            }

            Variable(compile, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ArrayCreate(CompileState compile, bool canAssign)
        {
            Parser parser = compile.Parser;
            int arrayIndex = compile.Parser.Module.VariableIndexes[nameof(Array)];
            EmitOpWithShortArg(compile, OpCode.GET_MODULE_VAR, arrayIndex);
            // initialization list
            byte argCount = 0;
            if (!parser.Check(TokenType.RIGHT_BRACKET))
            {
                do
                {
                    Expression(compile);
                    if (argCount > Byte.MaxValue)
                    {
                        throw new CompilerException(parser.Previous, "Can't have more than 255 initializer.");
                    }
                    ++argCount;
                } while (parser.Match(TokenType.COMMA));
            }
            parser.Consume(TokenType.RIGHT_BRACKET, "Expect ']' after initialization list.");

            EmitOpWithArg(compile, OpCode.CALL, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ArrayOrMapIndex(CompileState compile, bool canAssign)
        {
            Expression(compile);
            compile.Parser.Consume(TokenType.RIGHT_BRACKET, "Expect ']' after index.");
            if (canAssign && compile.Parser.Match(TokenType.EQUAL))
            {
                Expression(compile);
                EmitOp(compile, OpCode.SET_INDEX);
            }
            else
            {
                EmitOp(compile, OpCode.GET_INDEX);
            }
        }

        #endregion

        #region Parse expression or statement method

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Block(CompileState compile)
        {
            Parser parser = compile.Parser; 
            while (!parser.Check(TokenType.RIGHT_BRACE) && !parser.Check(TokenType.EOF))
            {
                Definition(compile);
            }
            parser.Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        }

        /// <summary>
        /// We simply parse the lowest precedence level, 
        /// which subsumes all of the higher-precedence expressions too. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Expression(CompileState compile)
        {
            ParsePrecedence(compile, Precedence.Assignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExpressionStatement(CompileState compile)
        {
            Expression(compile);
            compile.Parser.Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            EmitOp(compile, OpCode.POP);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Statement(CompileState compile)
        {
            Parser parser = compile.Parser;
            if (parser.Match(TokenType.IF))
            {
                IfStatement(compile);
            }
            else if (parser.Match(TokenType.WHILE))
            {
                WhileStatement(compile);
            }
            else if (parser.Match(TokenType.FOR))
            {
                ForStatement(compile);
            }
            else if (parser.Match(TokenType.LEFT_BRACE))
            {
                BeginScope(compile);
                Block(compile);
                EndScope(compile);
            }
            else if (parser.Match(TokenType.CONTINUE))
            {
                ContinueStatement(compile);
            }
            else if (parser.Match(TokenType.RETURN))
            {
                ReturnStatement(compile);
            }
            else if (parser.Match(TokenType.BREAK))
            {
                BreakStatement(compile);
            }
            else
            {
                ExpressionStatement(compile);
            }
        }


        private static void ReturnStatement(CompileState compile)
        {
            Parser parser = compile.Parser; 
            if (compile.FunctionType == FunctionType.Moudle)
            {
                throw new CompilerException(parser.Previous, "Can't return from top-level code.");
            }

            if (parser.Match(TokenType.SEMICOLON))
            {
                EmitReturn(compile);
            }
            else
            {
                if (compile.FunctionType == FunctionType.Initialized)
                {
                    throw new CompilerException(parser.Previous, "Can't return a value from an initializer");
                }

                Expression(compile);
                parser.Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
                EmitOp(compile, OpCode.RETURN);
            }
        }


        private static void ContinueStatement(CompileState compile)
        {
            if (compile.LoopStates.Count == 0)
            {
                throw new CompilerException(compile.Parser.Previous, "Can't use 'continue' outside of a loop.");
            }

            compile.Parser.Consume(TokenType.SEMICOLON, "Expect ';' after 'continue'.");

            // Discard any locals created inside the loop.
            for (int i = compile.LocalVars.Count - 1;
                i >= 0 && compile.LocalVars[i].Depth > compile.LoopStates.Peek().ScopeDepth;
                --i)
            {
                EmitOp(compile, OpCode.POP);
            }

            // Jump to top of current innermost loop.
            EmitLoop(compile, compile.LoopStates.Peek().LoopStart);
        }


        private static void BreakStatement(CompileState compile)
        {
            if (compile.LoopStates.Count == 0)
            {
                throw new CompilerException(compile.Parser.Previous, "Can't use 'continue' outside of a loop.");
            }

            compile.Parser.Consume(TokenType.SEMICOLON, "Expect ';' after 'break'.");

            // Discard any locals created inside the loop.
            for (int i = compile.LocalVars.Count - 1;
                i >= 0 && compile.LocalVars[i].Depth > compile.LoopStates.Peek().ScopeDepth;
                --i)
            {
                EmitOp(compile, OpCode.POP);
            }


            int exitJumpStart = EmitJump(compile, OpCode.JUMP);
            compile.LoopStates.Peek().BreakJumpStarts.Add(exitJumpStart);
        }


        private static void WhileStatement(CompileState compile)
        {

            CompileState.LoopState loopState = new()
            {
                LoopStart = compile.Function.Chunk.Instructions.Count,
                ScopeDepth = compile.ScopeDepth
            };
            compile.LoopStates.Push(loopState);

            compile.Parser.Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expression(compile);
            compile.Parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            int exitJumpStart = EmitJump(compile, OpCode.JUMP_IF_FALSE);

            EmitOp(compile, OpCode.POP);
            Statement(compile);
            EmitLoop(compile, compile.LoopStates.Peek().LoopStart);


            PatchJump(compile, exitJumpStart);
            EmitOp(compile, OpCode.POP);

            PatchLoopBreakJumps(compile, loopState);
            compile.LoopStates.Pop();
        }

        private static void ForStatement(CompileState compile)
        {
            Parser parser = compile.Parser; 
            BeginScope(compile);
            parser.Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            // First clause.
            if (parser.Match(TokenType.SEMICOLON))
            {
                // No initializer.
            }
            else if (parser.Match(TokenType.VAR))
            {
                VarDefinition(compile);
            }
            else
            {
                ExpressionStatement(compile);
            }

            CompileState.LoopState loopState = new()
            {
                LoopStart = compile.Function.Chunk.Instructions.Count,
                ScopeDepth = compile.ScopeDepth
            };
            compile.LoopStates.Push(loopState);

            // Second clause.
            int exitJump = -1;
            if (!parser.Match(TokenType.SEMICOLON))
            {
                Expression(compile);
                parser.Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");
                // Jump out of the loop if the condition is false.
                exitJump = EmitJump(compile, OpCode.JUMP_IF_FALSE);

                EmitOp(compile, OpCode.POP);
            }

            // Third clause
            if (!parser.Match(TokenType.RIGHT_PAREN))
            {
                int bodyJump = EmitJump(compile, OpCode.JUMP);
                int incrementStart = compile.Function.Chunk.Instructions.Count;
                Expression(compile);
                EmitOp(compile, OpCode.POP);
                parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

                EmitLoop(compile, compile.LoopStates.Peek().LoopStart);
                compile.LoopStates.Peek().LoopStart = incrementStart;
                PatchJump(compile, bodyJump);
            }

            Statement(compile);
            EmitLoop(compile, compile.LoopStates.Peek().LoopStart);

            if (exitJump != -1)
            {
                PatchJump(compile, exitJump);
                EmitOp(compile, OpCode.POP);
            }

            PatchLoopBreakJumps(compile, loopState);
            compile.LoopStates.Pop();

            EndScope(compile);
        }

        private static void IfStatement(CompileState compile)
        {
            Parser parser = compile.Parser;
            parser.Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expression(compile);
            parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            int thenJump = EmitJump(compile, OpCode.JUMP_IF_FALSE);
            EmitOp(compile, OpCode.POP);
            Statement(compile);

            int elseJump = EmitJump(compile, OpCode.JUMP);
            PatchJump(compile, thenJump);

            EmitOp(compile, OpCode.POP);
            if (parser.Match(TokenType.ELSE))
            {
                Statement(compile);
            }
            PatchJump(compile, elseJump);
        }

        private static void Definition(CompileState compile)
        {
            Parser parser = compile.Parser;
            if (parser.Match(TokenType.CLASS))
            {
                ClassDefinition(compile);
            }
            else if (parser.Match(TokenType.FUN))
            {
                FunDefinition(compile);
            }
            else if (parser.Match(TokenType.VAR))
            {
                VarDefinition(compile);
            }
            else if (parser.Match(TokenType.IMPORT))
            {
                ImportStatement(compile);  
            }
            else
            {
                Statement(compile);
            }
        }

        private static void ImportStatement(CompileState compile) 
        { 
            Parser parser = compile.Parser;

            int jumpStart = EmitJump(compile, OpCode.JUMP);
            int jumpAfter = compile.Function.Chunk.Instructions.Count;

            if (parser.Match(TokenType.STAR)) 
            {
                parser.Consume(TokenType.AS, "Expect as after '*'");
                int slot = DeclareNamedVariable(compile, "Expect variable name.");
                EmitOp(compile, OpCode.IMPORT_ALL_VARIABLE);
                DefineVariable(compile, slot);  
            }
            else
            {
                do
                {
                    parser.Consume(TokenType.IDENTIFIER, "Expect variable name.");
                    string sourceVarName = parser.Previous.Lexeme;
                    int sourceVarConst = AddConstant(compile, new Value(sourceVarName));

                    int slot;
                    if (parser.Match(TokenType.AS))
                    {
                        slot = DeclareNamedVariable(compile, "Expect variable name.");
                    }
                    else
                    {
                        slot = DeclareVariable(compile, sourceVarName);
                    }

                    EmitOpWithShortArg(compile, OpCode.IMPORT_VARIABLE, sourceVarConst);
                    DefineVariable(compile, slot);
                } while (parser.Match(TokenType.COMMA));
            }
            PatchJump(compile, jumpStart);
            parser.Consume(TokenType.FROM, "Expect 'from' after variables.");
            parser.Consume(TokenType.STRING, "Expect a module name.");
            int moduleNameConst = AddConstant(compile, new Value(parser.Previous.Lexeme));
            // Load the module by name.
            EmitOpWithShortArg(compile, OpCode.IMPORT_MODULE, moduleNameConst);
            EmitLoop(compile, jumpAfter);
        }

        private static void VarDefinition(CompileState compile)
        {
            Parser parser = compile.Parser;

            int global = DeclareNamedVariable(compile, "Expect variable name.");

            if (parser.Match(TokenType.EQUAL))
            {
                Expression(compile);
            }
            else
            {
                // Default initialize it to null. 
                EmitOp(compile, OpCode.NULL);
            }
            parser.Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");

            DefineVariable(compile, global);
        }

        private static void FunDefinition(CompileState compile)
        {
            int global = DeclareNamedVariable(compile, "Expect function name.");
            MarkLocalInitialized(compile);
            ParseFunction(compile, FunctionType.ModuleFunction);
            DefineVariable(compile, global);
        }

        private static void ClassDefinition(CompileState compile)
        {
            Parser parser = compile.Parser;

            parser.Consume(TokenType.IDENTIFIER, "Expect class name.");
            string className = parser.Previous.Lexeme;
            int nameConstIndex = AddConstant(compile, new Value(parser.Previous.Lexeme));
            int globalIndex = DeclareVariable(compile, className);

            EmitOpWithShortArg(compile, OpCode.DEFINE_CLASS, nameConstIndex);
            DefineVariable(compile, globalIndex);

            compile.EnclosingClass = new ClassCompileInfo();

            NamedVariable(compile, className, false);
            parser.Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

            while (!parser.Check(TokenType.RIGHT_BRACE) && !parser.Check(TokenType.EOF))
            {
                ParseClassMethod(compile);
            }

            parser.Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
            EmitOp(compile, OpCode.POP);

            compile.EnclosingClass = null;
        }

        private static void ParsePrecedence(CompileState compile, Precedence precedence)
        {
            Parser parser = compile.Parser;
            parser.Advance();

            ParseFunc? prefixRule = GetRule(parser.Previous.Type).Prefix;
            if (prefixRule == null)
            {
                Error(compile, "Expect expression.");
                return;
            }

            bool canAssign = precedence <= Precedence.Assignment;
            prefixRule(compile, canAssign);

            while (precedence <= GetRule(parser.Current.Type).Precedence)
            {
                parser.Advance();
                ParseFunc infixRule = GetRule(parser.Previous.Type).Infix!;
                infixRule(compile, canAssign);
            }
            if (canAssign && parser.Match(TokenType.EQUAL))
            {
                Error(compile, "Invalid assignment target.");
            }
        }

        #endregion

        #region assistant method 

        private static void ParseFunction(CompileState compile, FunctionType functionType)
        {
            CompileState currentCompile = new CompileState(compile.Parser, compile, functionType);
            Parser parser = compile.Parser; 
            BeginScope(currentCompile);

            parser.Consume(TokenType.LEFT_PAREN, "Expect '(' after function name.");

            if (!parser.Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    ++currentCompile.Function.Arity;
                    if (currentCompile.Function.Arity > 16)
                    {
                        Error(compile, "Can't have more than 255 parameters.");
                    }
                    int constantIndex = DeclareNamedVariable(currentCompile, "Expect parameter name.");
                    DefineVariable(currentCompile, constantIndex);
                } while (parser.Match(TokenType.COMMA));
            }

            parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
            parser.Consume(TokenType.LEFT_BRACE, "Expect '{' before function body.");

            Block(currentCompile);

            Function? compiledFunc = EndCompile(currentCompile);
            if (compiledFunc != null)
            {
                EmitConstant(compile, new Value(compiledFunc));
            }
        }

        private static byte ParseCallArgumentList(CompileState compile)
        {
            Parser parser = compile.Parser;
            byte argCount = 0;
            if (!parser.Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    Expression(compile);
                    if (argCount > Byte.MaxValue)
                    {
                        Error(compile, "Can't have more than 255 arguments.");
                    }
                    ++argCount;
                } while (parser.Match(TokenType.COMMA));
            }
            parser.Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return argCount;
        }

        private static void ParseClassMethod(CompileState compile)
        {
            Parser parser = compile.Parser; 
            parser.Consume(TokenType.IDENTIFIER, "Expect method name.");
            int nameConstIndx = AddConstant(compile, new Value(parser.Previous.Lexeme));

            FunctionType functionType = parser.Previous.Lexeme == "init" ?
                FunctionType.Initialized : FunctionType.ClassMethod;

            ParseFunction(compile, functionType);

            EmitOpWithShortArg(compile, OpCode.CLASS_METHOD, nameConstIndx);
        }

        private static void DefineVariable(CompileState compile, int global)
        {
            // Store the variable. If it's a local, the result of the initializer is
            // in the correct slot on the stack already so we're done.
            if (compile.ScopeDepth > 0)
            {
                MarkLocalInitialized(compile);
                return;
            }
            EmitOpWithShortArg(compile, OpCode.DEFINE_MODULE_VAR, global);
        }

        private static int DefineModuleVariable(Module module, string varName, Value val)
        {

            if (module.Variables.Count == MAX_MODULE_VARS)
            {
                return -1;
            }

            if (module.VariableIndexes.ContainsKey(varName))
            {
                return -2;
            }

            int index = module.Variables.Count;
            module.VariableIndexes[varName] = index;
            module.Variables.Add(val);
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DeclareNamedVariable(CompileState compile, string errorMessage)
        {
            compile.Parser.Consume(TokenType.IDENTIFIER, errorMessage);

            return DeclareVariable(compile, compile.Parser.Previous.Lexeme);
        }

        private static int DeclareVariable(CompileState compile, string varName)
        {
            if (varName.Length > MAX_VARIABLE_NAME)
            {
                Error(compile, $"Variable name cannot be longer than {MAX_VARIABLE_NAME} characters.");
            }

            // Top-level module scope.
            if (compile.ScopeDepth == 0)
            {
                int index =  DefineModuleVariable(compile.Parser.Module, varName, Value.Undefined(varName));

                if (index == -1)
                {
                    Error(compile, "Too many module variables defined.");
                }

                if (index == -2)
                {
                    Error(compile, "Module variable is already defined.");
                }
                return index;
            }

            for (int i = compile.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = compile.LocalVars[i];
                if (local.Depth != -1 && local.Depth < compile.ScopeDepth)
                {
                    break;
                }

                if (varName == local.Name)
                {
                    Error(compile, "Already a variable with this name in this scope.");
                    return i;
                }
            }

            if (compile.LocalVars.Count == MAX_LOCAL_VARS)
            {
                Error(compile, $"Cannot declare more than {MAX_LOCAL_VARS} variables in one scope.");
                return -1;  
            }
            return AddLocalVariable(compile, varName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MarkLocalInitialized(CompileState compile)
        {
            if (compile.ScopeDepth == 0)
            {
                return;
            }

            // mark variable initialized.
            compile.LocalVars[^1].Depth = compile.ScopeDepth;
        }

        /// <summary>
        /// Get the index of variables in the module.
        /// </summary>
        /// <returns> If the variable is not declared, return <see langword="-1"/> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ResolveModuleVar(Module module, string varName)
        {
            if (module.VariableIndexes.TryGetValue(varName, out var index))
            {
                return index;
            }
            else
            {
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AddLocalVariable(CompileState compile, string variableName)
        {
            LocalVariabal local = new(variableName, -1);
            int index = compile.LocalVars.Count;    
            compile.LocalVars.Add(local);
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ResolveLocalVar(CompileState compile, string varName)
        {
            for (int i = compile.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = compile.LocalVars[i];
                if (local.Name == varName)
                {
                    if (local.Depth == -1)
                    {
                        Error(compile, $"Use of unassigned local variable '{local.Name}'.");
                    }
                    return i;
                }
            }

            // we return -1 to signal that it wasn’t found
            // and should be assumed to be a global variable instead.
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PatchJump(CompileState compile, int offset)
        {
            var instructions = compile.Function.Chunk.Instructions;
            int jump = instructions.Count - offset - 2;

            if (jump > UInt16.MaxValue)
            {
                Error(compile, "Too much code to jump over.");
            }

            instructions[offset] = (byte)((jump >> 8) & 0xff);
            instructions[offset + 1] = (byte)(jump & 0xff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ClassCompileInfo? GetEnclosingClass(CompileState? compile)
        {
            while (compile != null)
            {
                if (compile.EnclosingClass != null) return compile.EnclosingClass;
                compile = compile.EnclosingCompile;
            }
            return null;
        }
        #endregion
    }
}
