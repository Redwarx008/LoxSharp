using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    using ParseFunc = Action<bool>;
    internal class Compiler
    {
        private enum FunctionType
        {
            Function,
            ClassMethod,
            Initialized,
            Script
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

        private class FunctionCompileState
        {
            internal class LoopState
            {
                public int LoopStart { get; set; } = 0;
                public int ScopeDepth { get; set; } = 0;
                public List<int> BreakJumpStarts { get; set; } = new();
            }
            public List<LocalVariabal> LocalVars { get; private set; }
            public Stack<LoopState> LoopStates { get; private set; }
            public int ScopeDepth { get; set; } = 0;
            public FunctionType FunctionType { get; private set; }
            public Function Function { get; private set; }

            public FunctionCompileState(FunctionType functionType)
            {
                LocalVars = new List<LocalVariabal>(16);
                LoopStates = new Stack<LoopState>();
                Function = new Function();
                FunctionType = functionType;

                // In the VM, stack slot 0 stores the calling function. 
                if (functionType == FunctionType.ClassMethod || functionType == FunctionType.Initialized)
                {
                    LocalVars.Add(new LocalVariabal("this", 0));
                }
                else
                {
                    LocalVars.Add(new LocalVariabal("PlaceHolder", 0));
                }
            }
        }
        internal struct ClassCompileState
        {

        }

        private readonly ParseRule[] _rules;

        private Token _previousToken;
        private Token _currentToken;
        private int _tokenIndex;
        private List<Token>? _tokens;
        private Stack<FunctionCompileState> _functionStates;
        private Stack<ClassCompileState> _classStates;

        private FunctionCompileState CurrentFunctionState => _functionStates.Peek();
        private Chunk CurrentChunk => CurrentFunctionState.Function.Chunk;

        private Dictionary<string, int> _globalValueIndexs;
        private List<Value> _globalValues;

        public Compiler(List<Value> globalValues, Dictionary<string, int> globalValueIndexs)
        {
            _classStates = new Stack<ClassCompileState>();
            _functionStates = new Stack<FunctionCompileState>();
            _globalValueIndexs = globalValueIndexs;
            _globalValues = globalValues;

            _rules = new ParseRule[Enum.GetValues(typeof(TokenType)).Length];

            _rules[(int)TokenType.LEFT_PAREN] = new ParseRule(Grouping, Call, Precedence.Call);
            _rules[(int)TokenType.RIGHT_PAREN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACKET] = new ParseRule(BracketCreate, BracketIndex, Precedence.Call);
            _rules[(int)TokenType.RIGHT_BRACKET] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.COMMA] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.DOT] = new ParseRule(null, Dot, Precedence.Call);
            _rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            _rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            _rules[(int)TokenType.SEMICOLON] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            _rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EQUAL_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            _rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            _rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
            _rules[(int)TokenType.STRING] = new ParseRule(String, null, Precedence.None);
            _rules[(int)TokenType.NUMBER] = new ParseRule(Number, null, Precedence.None);
            _rules[(int)TokenType.AND] = new ParseRule(null, And, Precedence.And);
            _rules[(int)TokenType.CLASS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.ELSE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.FOR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FUN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.IF] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.NIL] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.OR] = new ParseRule(null, Or, Precedence.Or);
            _rules[(int)TokenType.PRINT] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RETURN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SUPER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.THIS] = new ParseRule(This, null, Precedence.None);
            _rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.VAR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.WHILE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EOF] = new ParseRule(null, null, Precedence.None);
        }

        public CompiledScript Compile(List<Token> tokens)
        {
            _tokens = tokens;

            BeginCompilerState(FunctionType.Script);

            Advance();
            while (_currentToken.Type != TokenType.EOF)
            {
                Declaration();
            }

            Function topFunction = EndCompilerState();
            CompiledScript compiled = new(topFunction);
            Reset();
            return compiled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            _currentToken = default;
            _previousToken = default;
            _tokenIndex = 0;
            _tokens = null;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginScope()
        {
            ++CurrentFunctionState.ScopeDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndScope()
        {
            --CurrentFunctionState.ScopeDepth;

            while (CurrentFunctionState.LocalVars.Count > 0 &&
                CurrentFunctionState.LocalVars[CurrentFunctionState.LocalVars.Count - 1].Depth >
                CurrentFunctionState.ScopeDepth)
            {
                EmitBytes((byte)OpCode.POP);
                CurrentFunctionState.LocalVars.RemoveAt(CurrentFunctionState.LocalVars.Count - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginCompilerState(FunctionType functionType)
        {
            FunctionCompileState compilerState = new(functionType);
            if (functionType != FunctionType.Script)
            {
                compilerState.Function.Name = _previousToken.Lexeme;
            }
            else
            {
                compilerState.Function.Name = "Main";
            }

            _functionStates.Push(compilerState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Function EndCompilerState()
        {
#if DEBUG
            Disassembler disassembler = Disassembler.Instance;
            disassembler.DisassembleFunction(_functionStates.Peek().Function, _globalValues);
            Console.Write(disassembler.GetText());
#endif
            EmitReturn();
            return _functionStates.Pop().Function;
        }

        #region utility method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance()
        {
            Debug.Assert(_tokens != null);

            _previousToken = _currentToken;
            _currentToken = _tokens[_tokenIndex];
            ++_tokenIndex;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consume(TokenType type, string message)
        {
            if (_currentToken.Type == type)
            {
                Advance();
            }
            else
            {
                throw new CompilerException(_currentToken, message);
            }
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Check(TokenType type)
        {
            return _currentToken.Type == type;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(TokenType type)
        {
            if (!Check(type))
            {
                return false;
            }
            Advance();
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; ++i)
            {
                CurrentChunk.WriteByte(b[i], _previousToken.Line);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EmitJump(OpCode instruction)
        {
            EmitBytes((byte)instruction, 0xff, 0xff);
            return CurrentChunk.Instructions.Count - 1 - 2 + 1;
        }

        private void PatchLoopBreakJumps(FunctionCompileState.LoopState loopState)
        {
            for (int i = 0; i < loopState.BreakJumpStarts.Count; ++i)
            {
                PatchJump(loopState.BreakJumpStarts[i]);
            }
        }

        private void EmitLoop(int loopStart)
        {
            EmitBytes((byte)OpCode.LOOP);

            int offset = CurrentChunk.Instructions.Count - loopStart + 2;
            if (offset > ushort.MaxValue)
            {
                throw new CompilerException(_previousToken, "Loop body too large.");
            }

            byte high = (byte)((offset >> 8) & 0xff);
            byte low = (byte)(offset & 0xff);
            EmitBytes(high, low);
        }

        private void EmitReturn()
        {
            if (CurrentFunctionState.FunctionType == FunctionType.Initialized)
            {
                EmitBytes((byte)OpCode.GET_LOCAL, 0);
            }
            else
            {
                EmitBytes((byte)OpCode.NIL);
            }

            EmitBytes((byte)OpCode.RETURN);
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private void EmitConstant(Value val)
        {
            EmitBytes((byte)OpCode.CONSTANT_8, MakeConstant(val));
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        private byte MakeConstant(Value val)
        {
            int index = CurrentChunk.AddConstant(val);
            if (index > byte.MaxValue)
            {
                throw new CompilerException(_previousToken, "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParseRule GetRule(TokenType type)
        {
            return _rules[(int)type];
        }

        #endregion

        #region Parse literal method

        private void Literal(bool canAssign)
        {
            switch (_previousToken.Type)
            {
                case TokenType.FALSE:
                    EmitBytes((byte)OpCode.FALSE);
                    break;
                case TokenType.TRUE:
                    EmitBytes((byte)OpCode.TRUE);
                    break;
                case TokenType.NIL:
                    EmitBytes((byte)OpCode.NIL);
                    break;
                default: return; // Unreachable.
            }
        }
        private void Number(bool canAssign)
        {
            double value = Double.Parse(_previousToken.Lexeme);
            EmitConstant(new Value(value));
        }

        private void String(bool canAssign)
        {
            EmitConstant(new Value(_previousToken.Lexeme));
        }

        private void Variable(bool canAssign)
        {
            NamedVariable(_previousToken.Lexeme, canAssign);
        }

        private void NamedVariable(string variableName, bool canAssign)
        {
            OpCode getOp, setOp;

            int index = ResolveLocalVar(CurrentFunctionState, variableName);
            if (index != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                index = GetIdentifierIndex(variableName);
                getOp = OpCode.GET_GLOBAL;
                setOp = OpCode.SET_GLOBAL;
            }

            if (canAssign && Match(TokenType.EQUAL))
            {
                Expression();
                EmitBytes((byte)setOp, (byte)index);
            }
            else
            {
                EmitBytes((byte)getOp, (byte)index);
            }
        }
        private void Unary(bool canAssign)
        {
            TokenType operatorType = _previousToken.Type;

            // Compile the operand.
            ParsePrecedence(Precedence.Unary);

            switch (operatorType)
            {
                case TokenType.BANG:
                    EmitBytes((byte)OpCode.NOT);
                    break;
                case TokenType.MINUS:
                    EmitBytes((byte)OpCode.NEGATE);
                    break;
                default:
                    return;// Unreachable.
            }
        }

        private void Binary(bool canAssign)
        {
            TokenType operatorType = _previousToken.Type;
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence(rule.Precedence + 1);

            switch (operatorType)
            {
                case TokenType.BANG_EQUAL:
                    EmitBytes((byte)OpCode.EQUAL, (byte)OpCode.NOT);
                    break;
                case TokenType.EQUAL_EQUAL:
                    EmitBytes((byte)OpCode.EQUAL);
                    break;
                case TokenType.GREATER:
                    EmitBytes((byte)OpCode.GREATER);
                    break;
                case TokenType.GREATER_EQUAL:
                    EmitBytes((byte)OpCode.LESS, (byte)OpCode.NOT);
                    break;
                case TokenType.LESS:
                    EmitBytes((byte)OpCode.LESS);
                    break;
                case TokenType.LESS_EQUAL:
                    EmitBytes((byte)OpCode.GREATER, (byte)OpCode.NOT);
                    break;
                case TokenType.PLUS:
                    EmitBytes((byte)OpCode.ADD);
                    break;
                case TokenType.MINUS:
                    EmitBytes((byte)OpCode.SUBTRACT);
                    break;
                case TokenType.STAR:
                    EmitBytes((byte)OpCode.MULTIPLY);
                    break;
                case TokenType.SLASH:
                    EmitBytes((byte)OpCode.DIVIDE);
                    break;
                default: return; // Unreachable.
            }
        }

        private void Call(bool canAssgin)
        {
            byte argCount = ParseCallArgumentList();
            EmitBytes((byte)OpCode.CALL, argCount);
        }

        private void Dot(bool canAssign)
        {
            Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte nameConstIndex = MakeConstant(new Value(_previousToken.Lexeme));

            if (canAssign && Match(TokenType.EQUAL))
            {
                Expression();
                EmitBytes((byte)OpCode.SET_PROPERTY, nameConstIndex);
            }
            else if (Match(TokenType.LEFT_PAREN)) // direct invoke
            {
                byte argCount = ParseCallArgumentList();
                EmitBytes((byte)OpCode.INVOKE, nameConstIndex, argCount);
            }
            else
            {
                EmitBytes((byte)OpCode.GET_PROPERTY, nameConstIndex);
            }
        }

        private void Grouping(bool canAssign)
        {
            Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        }

        private void And(bool canAssign)
        {
            int endJump = EmitJump(OpCode.JUMP_IF_FALSE);

            EmitBytes((byte)OpCode.POP);
            ParsePrecedence(Precedence.And);

            PatchJump(endJump);
        }

        private void Or(bool canAssign)
        {
            int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = EmitJump(OpCode.JUMP);

            PatchJump(elseJump);
            EmitBytes((byte)OpCode.POP);

            ParsePrecedence(Precedence.Or);
            PatchJump(endJump);
        }

        private void This(bool canAssign)
        {
            if (_classStates.Count == 0)
            {
                throw new CompilerException(_previousToken, "Can't use 'this' outside of a class.");
            }

            Variable(false);
        }

        private void BracketCreate(bool canAssign)
        {
            int arrayIndex = _globalValueIndexs[nameof(Array)];
            EmitBytes((byte)OpCode.GET_GLOBAL, (byte)arrayIndex);
            // initialization list
            byte argCount = 0;
            if (!Check(TokenType.RIGHT_BRACKET))
            {
                do
                {
                    Expression();
                    if (argCount > Byte.MaxValue)
                    {
                        throw new CompilerException(_previousToken, "Can't have more than 255 initializer.");
                    }
                    ++argCount;
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RIGHT_BRACKET, "Expect ']' after initialization list.");

            EmitBytes((byte)OpCode.CALL, argCount);
        }

        private void BracketIndex(bool canAssign)
        {
            Expression();
            Consume(TokenType.RIGHT_BRACKET, "Expect ']' after index.");
            if (canAssign && Match(TokenType.EQUAL))
            {
                Expression();
                EmitBytes((byte)OpCode.SET_INDEX);
            }
            else
            {
                EmitBytes((byte)OpCode.GET_INDEX);
            }
        }

        #endregion

        #region Parse expression or statement method

        private void Block()
        {
            while (!Check(TokenType.RIGHT_BRACE) && !Check(TokenType.EOF))
            {
                Declaration();
            }
            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        }

        /// <summary>
        /// We simply parse the lowest precedence level, 
        /// which subsumes all of the higher-precedence expressions too. 
        /// </summary>
        private void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        private void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            EmitBytes((byte)OpCode.POP);
        }
        private void Statement()
        {
            if (Match(TokenType.IF))
            {
                IfStatement();
            }
            else if (Match(TokenType.WHILE))
            {
                WhileStatement();
            }
            else if (Match(TokenType.FOR))
            {
                ForStatement();
            }
            else if (Match(TokenType.LEFT_BRACE))
            {
                BeginScope();
                Block();
                EndScope();
            }
            else if (Match(TokenType.CONTINUE))
            {
                ContinueStatement();
            }
            else if (Match(TokenType.RETURN))
            {
                ReturnStatement();
            }
            else if (Match(TokenType.BREAK))
            {
                BreakStatement();
            }
            else
            {
                ExpressionStatement();
            }
        }
        private void ReturnStatement()
        {
            if (CurrentFunctionState.FunctionType == FunctionType.Script)
            {
                throw new CompilerException(_previousToken, "Can't return from top-level code.");
            }

            if (Match(TokenType.SEMICOLON))
            {
                EmitReturn();
            }
            else
            {
                if (CurrentFunctionState.FunctionType == FunctionType.Initialized)
                {
                    throw new CompilerException(_previousToken, "Can't return a value from an initializer");
                }

                Expression();
                Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
                EmitBytes((byte)OpCode.RETURN);
            }
        }

        private void ContinueStatement()
        {
            if (CurrentFunctionState.LoopStates.Count == 0)
            {
                throw new CompilerException(_previousToken, "Can't use 'continue' outside of a loop.");
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after 'continue'.");

            // Discard any locals created inside the loop.
            for (int i = CurrentFunctionState.LocalVars.Count - 1;
                i >= 0 && CurrentFunctionState.LocalVars[i].Depth > CurrentFunctionState.LoopStates.Peek().ScopeDepth;
                --i)
            {
                EmitBytes((byte)OpCode.POP);
            }

            // Jump to top of current innermost loop.
            EmitLoop(CurrentFunctionState.LoopStates.Peek().LoopStart);
        }

        private void BreakStatement()
        {
            if (CurrentFunctionState.LoopStates.Count == 0)
            {
                throw new CompilerException(_previousToken, "Can't use 'continue' outside of a loop.");
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after 'break'.");

            // Discard any locals created inside the loop.
            for (int i = CurrentFunctionState.LocalVars.Count - 1;
                i >= 0 && CurrentFunctionState.LocalVars[i].Depth > CurrentFunctionState.LoopStates.Peek().ScopeDepth;
                --i)
            {
                EmitBytes((byte)OpCode.POP);
            }


            int exitJumpStart = EmitJump(OpCode.JUMP);
            CurrentFunctionState.LoopStates.Peek().BreakJumpStarts.Add(exitJumpStart);
        }

        private void WhileStatement()
        {
            Debug.Assert(CurrentFunctionState != null);

            FunctionCompileState.LoopState loopState = new()
            {
                LoopStart = CurrentChunk.Instructions.Count,
                ScopeDepth = CurrentFunctionState.ScopeDepth
            };
            CurrentFunctionState.LoopStates.Push(loopState);

            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            int exitJumpStart = EmitJump(OpCode.JUMP_IF_FALSE);

            EmitBytes((byte)OpCode.POP);
            Statement();
            EmitLoop(CurrentFunctionState.LoopStates.Peek().LoopStart);


            PatchJump(exitJumpStart);
            EmitBytes((byte)OpCode.POP);

            PatchLoopBreakJumps(loopState);
            CurrentFunctionState.LoopStates.Pop();
        }

        private void ForStatement()
        {
            Debug.Assert(CurrentFunctionState != null);

            BeginScope();
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            // First clause.
            if (Match(TokenType.SEMICOLON))
            {
                // No initializer.
            }
            else if (Match(TokenType.VAR))
            {
                VarDeclaration();
            }
            else
            {
                ExpressionStatement();
            }

            FunctionCompileState.LoopState loopState = new()
            {
                LoopStart = CurrentChunk.Instructions.Count,
                ScopeDepth = CurrentFunctionState.ScopeDepth
            };
            CurrentFunctionState.LoopStates.Push(loopState);

            // Second clause.
            int exitJump = -1;
            if (!Match(TokenType.SEMICOLON))
            {
                Expression();
                Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");
                // Jump out of the loop if the condition is false.
                exitJump = EmitJump(OpCode.JUMP_IF_FALSE);

                EmitBytes((byte)OpCode.POP);
            }

            // Third clause
            if (!Match(TokenType.RIGHT_PAREN))
            {
                int bodyJump = EmitJump(OpCode.JUMP);
                int incrementStart = CurrentChunk.Instructions.Count;
                Expression();
                EmitBytes((byte)OpCode.POP);
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

                EmitLoop(CurrentFunctionState.LoopStates.Peek().LoopStart);
                CurrentFunctionState.LoopStates.Peek().LoopStart = incrementStart;
                PatchJump(bodyJump);
            }

            Statement();
            EmitLoop(CurrentFunctionState.LoopStates.Peek().LoopStart);

            if (exitJump != -1)
            {
                PatchJump(exitJump);
                EmitBytes((byte)OpCode.POP);
            }

            PatchLoopBreakJumps(loopState);
            CurrentFunctionState.LoopStates.Pop();

            EndScope();
        }

        private void IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");

            int thenJump = EmitJump(OpCode.JUMP_IF_FALSE);
            EmitBytes((byte)OpCode.POP);
            Statement();

            int elseJump = EmitJump(OpCode.JUMP);
            PatchJump(thenJump);

            EmitBytes((byte)OpCode.POP);
            if (Match(TokenType.ELSE))
            {
                Statement();
            }
            PatchJump(elseJump);
        }

        private void Declaration()
        {
            if (Match(TokenType.CLASS))
            {
                ClassDeclaration();
            }
            else if (Match(TokenType.FUN))
            {
                FunDeclaration();
            }
            else if (Match(TokenType.VAR))
            {
                VarDeclaration();
            }
            else
            {
                Statement();
            }
        }

        private void VarDeclaration()
        {
            byte global = ParseVariable("Expect variable name.");

            if (Match(TokenType.EQUAL))
            {
                Expression();
            }
            else
            {
                EmitBytes((byte)OpCode.NIL);
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            DefineVariable(global);
        }

        private void FunDeclaration()
        {
            byte global = ParseVariable("Expect function name.");
            MarkLocalInitialized();
            ParseFunction(FunctionType.Function);
            DefineVariable(global);
        }

        private void ClassDeclaration()
        {
            Consume(TokenType.IDENTIFIER, "Expect class name.");
            string className = _previousToken.Lexeme;
            byte nameConstIndex = MakeConstant(new Value(_previousToken.Lexeme));
            DeclareVariable();

            EmitBytes((byte)OpCode.CLASS, nameConstIndex);
            DefineVariable(GetIdentifierIndex(className));

            _classStates.Push(new ClassCompileState());

            NamedVariable(className, false);
            Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

            while (!Check(TokenType.RIGHT_BRACE) && !Check(TokenType.EOF))
            {
                ParseClassMethod();
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
            EmitBytes((byte)OpCode.POP);

            _classStates.Pop();
        }

        private void ParsePrecedence(Precedence precedence)
        {
            Advance();

            ParseFunc? prefixRule = GetRule(_previousToken.Type).Prefix;
            if (prefixRule == null)
            {
                throw new CompilerException(_previousToken, "Expect expression.");
            }

            bool canAssign = precedence <= Precedence.Assignment;
            prefixRule(canAssign);

            while (precedence <= GetRule(_currentToken.Type).Precedence)
            {
                Advance();
                ParseFunc infixRule = GetRule(_previousToken.Type).Infix!;
                infixRule(canAssign);
            }
            if (canAssign && Match(TokenType.EQUAL))
            {
                throw new CompilerException(_previousToken, "Invalid assignment target.");
            }
        }

        #endregion

        #region assistant method 

        private byte ParseVariable(string errorMessage)
        {
            Consume(TokenType.IDENTIFIER, errorMessage);

            DeclareVariable();
            if (CurrentFunctionState.ScopeDepth > 0)
            {
                return 0;
            }

            return GetIdentifierIndex(_previousToken.Lexeme);
        }

        private void ParseFunction(FunctionType functionType)
        {
            BeginCompilerState(functionType);

            BeginScope();

            Consume(TokenType.LEFT_PAREN, "Expect '(' after function name.");

            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    ++CurrentFunctionState.Function.Arity;
                    if (CurrentFunctionState.Function.Arity > Byte.MaxValue)
                    {
                        throw new CompilerException(_previousToken, "Can't have more than 255 parameters.");
                    }
                    byte constantIndex = ParseVariable("Expect parameter name.");
                    DefineVariable(constantIndex);
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
            Consume(TokenType.LEFT_BRACE, "Expect '{' before function body.");

            Block();

            Function currentFunc = EndCompilerState();
            EmitConstant(new Value(currentFunc));
        }

        private byte ParseCallArgumentList()
        {
            byte argCount = 0;
            if (!Check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    Expression();
                    if (argCount > Byte.MaxValue)
                    {
                        throw new CompilerException(_previousToken, "Can't have more than 255 arguments.");
                    }
                    ++argCount;
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return argCount;
        }

        private void ParseClassMethod()
        {
            Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte nameConstIndx = MakeConstant(new Value(_previousToken.Lexeme));

            FunctionType functionType = _previousToken.Lexeme == "init" ?
                FunctionType.Initialized : FunctionType.ClassMethod;

            ParseFunction(functionType);

            EmitBytes((byte)OpCode.CLASS_METHOD, nameConstIndx);
        }

        private void DefineVariable(byte global)
        {
            if (CurrentFunctionState.ScopeDepth > 0)
            {
                // mark variable initialized.
                MarkLocalInitialized();
                return;
            }
            EmitBytes((byte)OpCode.DEFINE_GLOBAL, global);
        }

        private void DeclareVariable()
        {
            if (CurrentFunctionState.ScopeDepth == 0)
            {
                return;
            }

            for (int i = CurrentFunctionState.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = CurrentFunctionState.LocalVars[i];
                if (local.Depth != -1 && local.Depth < CurrentFunctionState.ScopeDepth)
                {
                    break;
                }

                if (_previousToken.Lexeme == local.Name)
                {
                    throw new CompilerException(_previousToken, "Already a variable with this name in this scope.");
                }
            }

            AddLocalVariable(_previousToken.Lexeme);
        }

        private void MarkLocalInitialized()
        {
            if (CurrentFunctionState.ScopeDepth == 0)
            {
                return;
            }

            // mark variable initialized.
            CurrentFunctionState.LocalVars[CurrentFunctionState.LocalVars.Count - 1].Depth = CurrentFunctionState.ScopeDepth;
        }

        private byte GetIdentifierIndex(string name)
        {
            if (_globalValueIndexs.TryGetValue(name, out var index))
            {
                return (byte)index;
            }

            int newIndex = _globalValues.Count;
            _globalValues.Add(Value.Undefined(name));
            _globalValueIndexs.Add(name, newIndex);
            return (byte)newIndex;
        }

        private void AddLocalVariable(string variableName)
        {
            if (CurrentFunctionState.LocalVars.Count == Byte.MaxValue + 1)
            {
                throw new CompilerException(_previousToken, "Too many local variables in function.");
            }

            LocalVariabal local = new(variableName, -1);
            CurrentFunctionState.LocalVars.Add(local);
        }

        private int ResolveLocalVar(FunctionCompileState state, string varName)
        {
            for (int i = state.LocalVars.Count - 1; i >= 0; --i)
            {
                LocalVariabal local = state.LocalVars[i];
                if (local.Name == varName)
                {
                    if (local.Depth == -1)
                    {
                        throw new CompilerException(_previousToken,
                            "Can't read local variable in its own initializer.");
                    }
                    return i;
                }
            }

            // we return -1 to signal that it wasn’t found
            // and should be assumed to be a global variable instead.
            return -1;
        }

        private void PatchJump(int offset)
        {
            int jump = CurrentChunk.Instructions.Count - offset - 2;

            if (jump > UInt16.MaxValue)
            {
                throw new CompilerException(_previousToken, "Too much code to jump over.");
            }

            CurrentChunk.Instructions[offset] = (byte)((jump >> 8) & 0xff);
            CurrentChunk.Instructions[offset + 1] = (byte)(jump & 0xff);
        }
        #endregion
    }
}
