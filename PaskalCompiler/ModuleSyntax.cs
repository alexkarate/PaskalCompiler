using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PaskalCompiler
{
    class ModuleSyntax
    {
        ModuleLexical lexer;
        CToken curSymbol;
        bool analyzed = false;

        List<Scope> scopes;
        CType intType, realType, boolType, charType, stringType, voidType, unknownType;
        public ModuleSyntax(ModuleLexical lexer)
        {
            this.lexer = lexer;
            curSymbol = lexer.NextSym();
        }

        public ModuleIO IO
        {
            get { return lexer.io; }
        }

        public ModuleSyntax(string filePath) : this(new ModuleLexical(new ModuleIO(filePath))) { }

        public string GenerateListing()
        {
            if(!analyzed)
                CheckProgram();
            return IO.GenerateListing();
        }

        void Accept(CToken token)
        {
            if (token.Equals(curSymbol))
            {
                curSymbol = lexer.NextSym();
            }
            else
            {
                throw new InvalidSymbolException(token, curSymbol);
            }
        }
        COperation Oper(EOperator op)
        {
            return new COperation(op);
        }
        private readonly CIdentificator _ident = new CIdentificator(string.Empty);
        CIdentificator Ident()
        {
            return _ident;
        }

        CValue Value(EVarType type)
        {
            return new CValue(type, null);
        }
        void AddBooleanConsts(CIdentInfo falseC, CIdentInfo trueC)
        {
            methodILGenerator.Emit(OpCodes.Ldc_I4_0);
            methodILGenerator.Emit(OpCodes.Stsfld, falseC.fb);
            methodILGenerator.Emit(OpCodes.Ldc_I4_1);
            methodILGenerator.Emit(OpCodes.Stsfld, trueC.fb);
        }
        void GenerateDefaultScope()
        {
            scopes = new List<Scope>();
            PushScope();

            intType = AddType(EType.et_integer);
            realType = AddType(EType.et_real);
            boolType = AddType(EType.et_boolean);
            charType = AddType(EType.et_char);
            stringType = AddType(EType.et_string);
            voidType = AddType(EType.et_void);
            unknownType = AddType(EType.et_unknown);

            AddIdentifier("integer", IdentUseType.iClass, intType);
            AddIdentifier("real", IdentUseType.iClass, realType);
            AddIdentifier("boolean", IdentUseType.iClass, boolType);
            AddIdentifier("char", IdentUseType.iClass, charType);
            AddIdentifier("string", IdentUseType.iClass, stringType);

            var falseConst = AddIdentifier("false", IdentUseType.iConst, boolType);
            var trueConst = AddIdentifier("true", IdentUseType.iConst, boolType);
            AddBooleanConsts(falseConst, trueConst);

            falseConst = AddIdentifier("False", IdentUseType.iConst, boolType);
            trueConst = AddIdentifier("True", IdentUseType.iConst, boolType);
            AddBooleanConsts(falseConst, trueConst);

            AddIdentifier("writeln", IdentUseType.iFunc, voidType);
        }
        int fieldCounter = 0;
        CIdentInfo AddIdentifier(string name, IdentUseType useType, CType type)
        {
            if(useType == IdentUseType.iClass || useType == IdentUseType.iFunc || type == unknownType || type == voidType)
                return scopes.Last().AddIdentifier(name, useType, type);

            FieldBuilder fb = typeBuilder.DefineField(name + '_' + fieldCounter++, type.OutputType, FieldAttributes.Static | FieldAttributes.Private);
            return scopes.Last().AddIdentifier(name, useType, type, fb);
        }

        CIdentInfo FindIdentifier(CToken token)
        {
            CIdentificator ident = token as CIdentificator;

            if (ident == null)
                return null;
            return FindIdentifier(ident.identName);
        }

        CIdentInfo FindIdentifier(string name)
        {
            for(int i = scopes.Count - 1; i >= 0; i--)
            {
                CIdentInfo ident = scopes[i].FindIdentifier(name);
                if (ident != null)
                    return ident;
            }
            return null;
        }

        CType AddType(EType type)
        {
            return scopes.Last().AddType(type);
        }

        void PushScope()
        {
            scopes.Add(new Scope());
        }
        void PopScope()
        {
            if(scopes.Count > 0)
                scopes.Remove(scopes.Last());
        }

        CToken[] operOrEndList;

        void CreateTokenLists()
        {
            operOrEndList = new CToken[] { 
                Ident(), 
                Oper(EOperator.ifsy), 
                Oper(EOperator.whilesy), 
                Oper(EOperator.beginsy), 
                Oper(EOperator.endsy) 
            };
        }

        const string ext = ".exe";

        AssemblyBuilder assemblyBuilder;
        ModuleBuilder moduleBuilder;
        TypeBuilder typeBuilder;
        MethodBuilder methodBuilder;
        ILGenerator methodILGenerator;
        void CreateGenerator()
        {
            AssemblyName aName = new AssemblyName("Program");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(aName.Name, aName.Name + ext);
            typeBuilder = moduleBuilder.DefineType("Program");
            methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Static | MethodAttributes.Public);
            methodILGenerator = methodBuilder.GetILGenerator();
        }

        void OutputProgram()
        {
            methodILGenerator.Emit(OpCodes.Ret);
            typeBuilder.CreateType();
            moduleBuilder.CreateGlobalFunctions();
            assemblyBuilder.SetEntryPoint(methodBuilder);
            assemblyBuilder.Save(assemblyBuilder.GetName().Name + ext);
        }

        public void CompileProgram()
        {
            if (!analyzed)
                CheckProgram();
            if (IO.Errors.Count == 0)
                OutputProgram();
        }

        void EmitRelation(EOperator op)
        {
            switch (op)
            {
                case EOperator.equals:
                    methodILGenerator.Emit(OpCodes.Ceq);
                    break;
                case EOperator.greater:
                    methodILGenerator.Emit(OpCodes.Cgt);
                    break;
                case EOperator.less:
                    methodILGenerator.Emit(OpCodes.Clt);
                    break;
                case EOperator.notequals:
                    methodILGenerator.Emit(OpCodes.Ceq);
                    methodILGenerator.Emit(OpCodes.Ldc_I4_0);
                    methodILGenerator.Emit(OpCodes.Ceq);
                    break;
                case EOperator.greaterequals:
                    methodILGenerator.Emit(OpCodes.Clt);
                    methodILGenerator.Emit(OpCodes.Ldc_I4_0);
                    methodILGenerator.Emit(OpCodes.Ceq);
                    break;
                case EOperator.lessequals:
                    methodILGenerator.Emit(OpCodes.Cgt);
                    methodILGenerator.Emit(OpCodes.Ldc_I4_0);
                    methodILGenerator.Emit(OpCodes.Ceq);
                    break;
                default:
                    methodILGenerator.Emit(OpCodes.Nop);
                    break;
            }
        }
        void EmitAdditive(EOperator op)
        {
            switch (op)
            {
                case EOperator.plus:
                    methodILGenerator.Emit(OpCodes.Add);
                    break;
                case EOperator.minus:
                    methodILGenerator.Emit(OpCodes.Sub);
                    break;
                case EOperator.orsy:
                    methodILGenerator.Emit(OpCodes.Or);
                    break;
                default:
                    methodILGenerator.Emit(OpCodes.Nop);
                    break;
            }
        }
        void EmitMultiplicative(EOperator op, CType rightType)
        {
            switch (op)
            {
                case EOperator.star:
                    methodILGenerator.Emit(OpCodes.Mul);
                    break;
                case EOperator.slash:
                    if (rightType._tt != EType.et_real)
                        EmitConvert(rightType, realType);
                    methodILGenerator.Emit(OpCodes.Div);
                    break;
                case EOperator.divsy:
                    methodILGenerator.Emit(OpCodes.Div);
                    break;
                case EOperator.modsy:
                    methodILGenerator.Emit(OpCodes.Rem);
                    break;
                case EOperator.andsy:
                    methodILGenerator.Emit(OpCodes.And);
                    break;
                default:
                    methodILGenerator.Emit(OpCodes.Nop);
                    break;
            }
        }
        void EmitConcat(CType l, CType r)
        {
            var concat = typeof(String).GetMethod("Concat", new Type[] { typeof(object), typeof(object) });
            if(r._tt != EType.et_string)
            {
                methodILGenerator.Emit(OpCodes.Box, r.OutputType);
            }
            if(l._tt != EType.et_string) 
            {
                var t = methodILGenerator.DeclareLocal(typeof(object));
                methodILGenerator.Emit(OpCodes.Stloc, t);
                methodILGenerator.Emit(OpCodes.Box, l.OutputType);
                methodILGenerator.Emit(OpCodes.Ldloc, t);
            }
            methodILGenerator.Emit(OpCodes.Call, concat);
        }
        void EmitWriteLine(CType expressionType)
        {
            Type exprType = expressionType.OutputType;
            if (exprType == typeof(void))
                return;
            var writeLine = typeof(Console).GetMethod("WriteLine", new Type[] { exprType });
            methodILGenerator.Emit(OpCodes.Call, writeLine);
        }
        void EmitConvert(CType from, CType to)
        {
            string methodName;
            switch (to._tt)
            {
                case EType.et_integer:
                    methodName = "ToInt32";
                    break;
                case EType.et_real:
                    methodName = "ToSingle";
                    break;
                case EType.et_boolean:
                    methodName = "ToBoolean";
                    break;
                case EType.et_char:
                    methodName = "ToUInt32";
                    break;
                case EType.et_string:
                    methodName = "ToString";
                    break;
                default:
                    return;
            }
            var convert = typeof(Convert).GetMethod(methodName, new Type[] { from.OutputType });
            methodILGenerator.Emit(OpCodes.Call, convert);
        }

        public void CheckProgram()
        {
            CreateGenerator();
            GenerateDefaultScope();
            CreateTokenLists();
            
            try
            {
                Program();
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
            }
            catch(Exception e)
            {
                IO.RecordError(e);
                Console.WriteLine(e);
            }
            if (curSymbol._tt != ETokenType.None)
                IO.RecordError(new UnexpectedTokenException(curSymbol));
            analyzed = true;
        }

        void SkipUntilToken(CToken[] token)
        {
            bool skip = true;
            while(skip)
            {
                for (int i = 0; i < token.Length; i++)
                {
                    if (curSymbol.Equals(token[i]))
                    {
                        skip = false;
                        break;
                    }
                }
                if (curSymbol.Equals(CToken.empty))
                    skip = false;

                if (skip)
                    curSymbol = lexer.NextSym();
            }
        }

        void Program()
        {
            PushScope();
            try
            {
                Accept(Oper(EOperator.programsy));
                Accept(Ident());
                
                if (curSymbol.Equals(Ident()))
                {
                    Accept(Ident());
                    while (curSymbol.Equals(Oper(EOperator.comma)))
                    {
                        Accept(Oper(EOperator.comma));
                        Accept(Ident());
                    }
                }
                
                Accept(Oper(EOperator.semicolon));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.varsy), Oper(EOperator.beginsy)});
            }

            Block();
            Accept(Oper(EOperator.dot));
        }
        void Block()
        {
            //Labels();
            //Constants();
            //Types();
            try
            {
                Variables();
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.beginsy), Oper(EOperator.dot) });
            }
            //Functions();
            try
            {
                Operators();
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.dot) });
            }
        }
        void Operators()
        {
            CompoundOperator();
        }
        void Operator()
        {
            if (curSymbol.Equals(Ident()))
            {
                CIdentInfo ident = FindIdentifier(curSymbol);
                if (ident == null)
                {
                    IO.RecordError(new UndeclaredIdentificatorException(curSymbol as CIdentificator));
                    ident = AddIdentifier((curSymbol as CIdentificator).identName, IdentUseType.iVar, unknownType);
                }
                else if(ident.useType == IdentUseType.iFunc && ident.name == "writeln")
                {
                    Accept(Ident());
                    Accept(Oper(EOperator.openBr));
                    
                    CType t = Expression();
                    EmitWriteLine(t);
                    
                    Accept(Oper(EOperator.closeBr));
                    return;
                }
                else if (ident.useType != IdentUseType.iVar)
                    IO.RecordError(new VariableNotFoundException(ident));

                CType l = ident.type;
                Accept(Ident());

                Accept(Oper(EOperator.assignSy));
                CType r = Expression();
                
                if (r != unknownType)
                {
                    if (l == unknownType)
                    {
                        ident.type = r;
                        l = r;
                    }
                    else if (!r.isDerivedTo(l))
                        IO.RecordError(new UnderivableTypeException(l, r));
                    else if(ident.fb != null)
                    {
                        if(r != l)
                        {
                            EmitConvert(r, l);
                        }
                        methodILGenerator.Emit(OpCodes.Stsfld, ident.fb);
                    }
                }
            }
            else if (curSymbol.Equals(Oper(EOperator.beginsy)))
            {
                CompoundOperator();
            }
            else if (curSymbol.Equals(Oper(EOperator.ifsy)))
            {
                ChooseOperator();
            }
            else if (curSymbol.Equals(Oper(EOperator.whilesy)))
            {
                PreLoopOperator();
            }
        }

        void CompoundOperator()
        {
            try
            {
                Accept(Oper(EOperator.beginsy));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.beginsy), Oper(EOperator.endsy) });
                
                if (curSymbol.Equals(Oper(EOperator.beginsy)))
                    Accept(Oper(EOperator.beginsy));
            }
            try
            {
                Operator();
            }
            catch (CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
            }
            if(!curSymbol.Equals(Oper(EOperator.semicolon)) && !curSymbol.Equals(Oper(EOperator.endsy)))
            {
                IO.RecordError(new InvalidSymbolException(Oper(EOperator.semicolon), curSymbol));
                SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
            }
            while(curSymbol.Equals(Oper(EOperator.semicolon)))
            {
                Accept(Oper(EOperator.semicolon)); 
                try
                {
                    Operator();
                }
                catch (CompilerException e)
                {
                    IO.RecordError(e);
                    SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
                }
                if(!curSymbol.Equals(Oper(EOperator.semicolon)) && !curSymbol.Equals(Oper(EOperator.endsy)))
                {
                    IO.RecordError(new InvalidSymbolException(Oper(EOperator.semicolon), curSymbol));
                    SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
                }
            }
            Accept(Oper(EOperator.endsy));
        }
        void ChooseOperator()
        {
            var falseLabel = methodILGenerator.DefineLabel();
            var endLabel = methodILGenerator.DefineLabel();
            
            Accept(Oper(EOperator.ifsy));
            CType t = Expression();
            
            if (t != unknownType && !t.isDerivedTo(boolType))
                IO.RecordError(new UnderivableTypeException(t, boolType));
            
            try
            {
                Accept(Oper(EOperator.thensy));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(operOrEndList);
            }
            methodILGenerator.Emit(OpCodes.Brfalse_S, falseLabel);
            
            Operator();
            if (curSymbol.Equals(Oper(EOperator.semicolon)))
                Accept(Oper(EOperator.semicolon));
            
            methodILGenerator.Emit(OpCodes.Br_S, endLabel);
            
            if (curSymbol.Equals(Oper(EOperator.elsesy)))
            {
                Accept(Oper(EOperator.elsesy));

                methodILGenerator.MarkLabel(falseLabel);
                Operator();
            }
            else
                methodILGenerator.MarkLabel(falseLabel);
            
            methodILGenerator.MarkLabel(endLabel);
        }

        void PreLoopOperator()
        {
            var startLabel = methodILGenerator.DefineLabel();
            var endLabel = methodILGenerator.DefineLabel();
            
            Accept(Oper(EOperator.whilesy));
            methodILGenerator.MarkLabel(startLabel);
            
            CType t = Expression();
            
            if (t != unknownType && !t.isDerivedTo(boolType))
                IO.RecordError(new UnderivableTypeException(t, boolType));
            
            methodILGenerator.Emit(OpCodes.Brfalse_S, endLabel);
            try
            {
                Accept(Oper(EOperator.dosy));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(operOrEndList);
            }

            Operator();
            
            methodILGenerator.Emit(OpCodes.Br_S, startLabel);
            methodILGenerator.MarkLabel(endLabel);
        }

        CType Expression()
        {
            CType l = SimpleExpression(), r = unknownType;
            COperation oper = curSymbol as COperation;
            
            if (oper != null && oper.IsRelative())
            {
                Accept(Oper(oper._vo));
                r = SimpleExpression();
                
                if (l != unknownType && r != unknownType)
                {
                    if (!r.isDerivedTo(l) && !l.isDerivedTo(r))
                        IO.RecordError(new UnderivableTypeException(l, r));
                    else if (!l.isDerivedTo(r, oper))
                        IO.RecordError(new UnderivableTypeException(l, r, oper));
                }
                l = boolType;

                EmitRelation(oper._vo);
            }
            return l;
        }
        CType SimpleExpression()
        {
            Sign();
            CType l = Term();
            COperation oper = curSymbol as COperation;
            
            while (oper != null && oper.IsAdditive())
            {
                Accept(Oper(oper._vo));
                CType r = Term();
                if (l == unknownType)
                    r = l;
                if (r == unknownType)
                    l = r;
                if(l != unknownType && r != unknownType)
                {
                    if (!l.isDerivedTo(r) && !r.isDerivedTo(l))
                        IO.RecordError(new UnderivableTypeException(l, r));
                    else if (!l.isDerivedTo(r, oper))
                        IO.RecordError(new UnderivableTypeException(l, r, oper));
                    
                    if (l != stringType && r != stringType)
                        EmitAdditive(oper._vo);
                    else
                        EmitConcat(l, r);
                    
                    if (l.isDerivedTo(r))
                        l = r;

                }
                oper = curSymbol as COperation;
            }
            return l;
        }

        CType Term()
        {
            CType l = Factor();
            COperation oper = curSymbol as COperation;
            
            while(oper != null && oper.IsMultiplicative())
            {
                Accept(Oper(oper._vo));
                CType r = Factor();
                if (l == unknownType)
                    r = l;
                
                if (r == unknownType)
                    l = r;
                
                if (l != unknownType && r != unknownType)
                {
                    if (l.isDerivedTo(r))
                        l = r;
                    else if (!r.isDerivedTo(l))
                        IO.RecordError(new UnderivableTypeException(l, r));
                    else if (!l.isDerivedTo(r, oper))
                        IO.RecordError(new UnderivableTypeException(l, r, oper));
                }
                EmitMultiplicative(oper._vo, r);
                oper = curSymbol as COperation;
            }
            return l;
        }

        CType Factor()
        {
            if (curSymbol.Equals(Ident()))
            {
                CIdentInfo ident = FindIdentifier(curSymbol);
                
                if (ident == null)
                {
                    IO.RecordError(new UndeclaredIdentificatorException(curSymbol as CIdentificator));
                    ident = AddIdentifier((curSymbol as CIdentificator).identName, IdentUseType.iVar, unknownType);
                }
                else if (ident.useType == IdentUseType.iClass)
                    IO.RecordError(new VariableNotFoundException(ident));
                else if(ident.fb != null)
                {
                    methodILGenerator.Emit(OpCodes.Ldsfld, ident.fb);
                }
                
                Accept(Ident());
                return ident.type;
            }
            else if (curSymbol.Equals(Oper(EOperator.openBr)))
            {
                CType t;
                Accept(Oper(EOperator.openBr));
                try
                {
                    t = Expression();
                }
                catch(CompilerException e)
                {
                    IO.RecordError(e);
                    SkipUntilToken(new CToken[] { Oper(EOperator.closeBr), Oper(EOperator.semicolon), Oper(EOperator.endsy) });
                    t = unknownType;
                }
                Accept(Oper(EOperator.closeBr));
                return t;
            }
            else if(curSymbol is CValue)
                return ConstNoSign(curSymbol as CValue);
            else
            {
                IO.RecordError(new InvalidExpressionException(curSymbol));
                return unknownType;
            }
        }

        CType ConstNoSign(CValue c)
        {
            switch (c._vt)
            {
                case EVarType.vtString:
                    methodILGenerator.Emit(OpCodes.Ldstr, (string)c.value);
                    Accept(Value(c._vt));
                    return stringType;
                
                case EVarType.vtChar:
                    methodILGenerator.Emit(OpCodes.Ldc_I4, Convert.ToUInt32((char)c.value));
                    Accept(Value(c._vt));
                    return charType;
                
                case EVarType.vtBoolean:
                    methodILGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32((bool)c.value));
                    Accept(Value(c._vt));
                    return boolType;
                
                default:
                    return NumberNoSign(c);
            }
        }
        CType NumberNoSign(CValue c)
        {
            switch (c._vt)
            {
                case EVarType.vtInt:
                    methodILGenerator.Emit(OpCodes.Ldc_I4, (int)c.value);
                    Accept(Value(EVarType.vtInt));
                    return intType;
                
                case EVarType.vtReal:
                    methodILGenerator.Emit(OpCodes.Ldc_R4, (float)c.value);
                    Accept(Value(EVarType.vtReal));
                    return realType;
                
                default:
                    IO.RecordError(new ExpectedNumberConstantException(c._vt));
                    return unknownType;
            }
        }

        void Sign()
        {
            if (curSymbol.Equals(Oper(EOperator.plus)))
                Accept(Oper(EOperator.plus));
            else if (curSymbol.Equals(Oper(EOperator.minus)))
                Accept(Oper(EOperator.minus));
        }

        void Variables()
        {
            if(curSymbol.Equals(Oper(EOperator.varsy)))
            {
                Accept(Oper(EOperator.varsy));
                SingleTypeVariable();
                Accept(Oper(EOperator.semicolon));
                
                while(curSymbol.Equals(Ident()))
                {
                    SingleTypeVariable();
                    Accept(Oper(EOperator.semicolon));
                }
            }
        }

        void SingleTypeVariable()
        {
            List<CToken> varList = new List<CToken>();
            varList.Add(curSymbol);
            try
            {
                Accept(Ident());

                while (curSymbol.Equals(Oper(EOperator.comma)))
                {
                    Accept(Oper(EOperator.comma));
                    CToken t = curSymbol;
                    Accept(Ident());
                    varList.Add(t);
                }
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.beginsy), Oper(EOperator.colon) });
            }
            try
            {
                Accept(Oper(EOperator.colon));
                var type = SingleType();
                
                CIdentificator identificator;
                for (int i = 0; i < varList.Count; i++)
                {
                    identificator = varList[i] as CIdentificator;
                    if (identificator == null)
                        IO.RecordError(new IdentificatorNotFoundException(varList[i]));
                    else if (FindIdentifier(identificator.identName) != null)
                        IO.RecordError(new IdentificatorAlreadyExistsException(identificator));
                    else    
                        AddIdentifier(identificator.identName, IdentUseType.iVar, type);
                }
            }
            catch(CompilerException e)
            {
                IO.RecordError(e);
                SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.beginsy) });
            }
        }
        CType SingleType()
        {
            return SimpleType();
        }
        CType SimpleType()
        {
            CToken t = curSymbol;
            Accept(Ident());
            CIdentInfo ident = FindIdentifier(t);
            
            if (ident == null || ident.useType != IdentUseType.iClass)
            {
                string name = (t as CIdentificator).identName;
                IO.RecordError(new TypeNotFoundException(name));
                return unknownType;
            }
            return ident.type;
        }
        
    }
    enum EType
    {
        et_integer,
        et_real,
        et_boolean,
        et_char,
        et_string,
        et_void,
        et_unknown
    }

    abstract class CType
    {
        public abstract bool isDerivedTo(CType b);
        public abstract bool isDerivedTo(CType b, COperation op);
        public EType _tt;
        public Type OutputType { get; protected set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            CType t = obj as CType;
            if (t == null)
                return false;
            return _tt == t._tt;
        }
        public static bool operator ==(CType l, CType r)
        {
            return l._tt == r._tt;
        }
        public static bool operator !=(CType l, CType r)
        {
            return l._tt != r._tt;
        }

    }

    class CIntType : CType
    {
        public CIntType()
        {
            _tt = EType.et_integer;
            OutputType = typeof(int);
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_integer || b._tt == EType.et_real || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (op.IsAdditive() || op.IsMultiplicative() || op.IsRelative())
                return true;
            return false;
        }
    }
    class CRealType : CType
    {
        public CRealType()
        {
            _tt = EType.et_real;
            OutputType = typeof(float);
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_real || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (op.IsBinary() || op.IsIntegerDivision())
                return false;
            if (op.IsAdditive() || op.IsMultiplicative() || op.IsRelative())
                return true;
            return false;
        }
    }

    class CBoolType : CType
    {
        public CBoolType()
        {
            _tt = EType.et_boolean;
            OutputType = typeof(bool);
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_boolean || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (op.IsBinary() || op.IsEquals())
                return true;
            return false;
        }
    }

    class CCharType : CType
    {
        public CCharType()
        {
            _tt = EType.et_char;
            OutputType = typeof(char);
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_char || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (op.IsBinary() || op.IsAdditive() || op.IsRelative())
                return true;
            return false;
        }
    }

    class CStringType : CType
    {
        public CStringType()
        {
            _tt = EType.et_string;
            OutputType = typeof(string);
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (op._vo == EOperator.plus || op.IsRelative())
                return true;
            return false;
        }
    }

    class CVoidType : CType
    {
        public CVoidType()
        {
            _tt = EType.et_void;
            OutputType = typeof(void);
        }
        public override bool isDerivedTo(CType b)
        {
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            return isDerivedTo(b);
        }
    }

    class CUnknownType : CType
    {
        public CUnknownType()
        {
            _tt = EType.et_unknown;
            OutputType = typeof(void);
        }
        public override bool isDerivedTo(CType b)
        {
            return true;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            return isDerivedTo(b);
        }
    }

    enum IdentUseType
    {
        iVar,
        iClass,
        iConst,
        iFunc
    }

    class CIdentInfo
    {
        public IdentUseType useType;
        public CType type;
        public string name;
        public FieldBuilder fb;
        public CIdentInfo(IdentUseType useType, CType type, string name, FieldBuilder fb)
        {
            this.useType = useType;
            this.type = type;
            this.name = name;
            this.fb = fb;
        }
    }

    class Scope
    {
        public Dictionary<string, CIdentInfo> identifierTable;
        public List<CType> typeTable;

        public Scope()
        {
            identifierTable = new Dictionary<string, CIdentInfo>();
            typeTable = new List<CType>();
        }

        public CIdentInfo FindIdentifier(string ident) 
        {
            CIdentInfo value;
            bool success = identifierTable.TryGetValue(ident, out value);
            if (success)
                return value;
            else
                return null;
        }
        public CIdentInfo AddIdentifier(string ident, IdentUseType useType, CType type, FieldBuilder fb)
        {
            CIdentInfo value = new CIdentInfo(useType, type, ident, fb);
            identifierTable.Add(ident, value);
            return value;
        }

        public CIdentInfo AddIdentifier(string ident, IdentUseType useType, CType type)
        {
            return AddIdentifier(ident, useType, type, null);
        }

        public CType AddType(EType type)
        {
            CType newType;
            switch (type)
            {
                case EType.et_integer:
                    newType = new CIntType();
                    break;
                case EType.et_real:
                    newType = new CRealType();
                    break;
                case EType.et_boolean:
                    newType = new CBoolType();
                    break;
                case EType.et_char:
                    newType = new CCharType();
                    break;
                case EType.et_string:
                    newType = new CStringType();
                    break;
                case EType.et_void:
                    newType = new CVoidType();
                    break;
                case EType.et_unknown:
                    newType = new CUnknownType();
                    break;
                default:
                    return null;
            }
            typeTable.Add(newType);
            return newType;
        }
    }
    class CompilerException : ApplicationException 
    {
        public CompilerException() : base("Generic compiler exception.") { }
        public CompilerException(string s) : base(s) { }
    }
    // Syntax Exceptions
    class SyntaxException : CompilerException
    {
        public SyntaxException() : base("Generic syntax exception.") { }
        public SyntaxException(string s) : base(s) { }
    }
    class InvalidSymbolException : SyntaxException
        {
        public CToken expected, current;
        public InvalidSymbolException(CToken expected, CToken current) : base(string.Format("Expected {0}, got {1}.", expected, current))
        {
            this.expected = expected;
            this.current = current;
        }
    }
    // Semantic Exceptions
    class SemanticException : CompilerException
    {
        public SemanticException() : base("Generic semantic exception.") { }
        public SemanticException(string s) : base(s) { }
    }
    class UnderivableTypeException : SemanticException
    {
        public UnderivableTypeException(CType l, CType r) : base(string.Format("Type {0} is not derivable to {1}.", r._tt, l._tt)) { }
        public UnderivableTypeException(CType l, CType r, COperation op) : base(string.Format("{0} cannot be applied to {1} and {2}.", op, l._tt, r._tt)) { }
    }
    class UndeclaredIdentificatorException : SemanticException
    {
        public UndeclaredIdentificatorException(CIdentificator ident) : base(string.Format("Identificator {0} not found.", ident.identName)) { }
    }

    class VariableNotFoundException : SemanticException
    {
        public VariableNotFoundException(CIdentInfo info) : base(string.Format("Expected to find variable, found {0}.", info.type)) { }
    }

    class IdentificatorNotFoundException : SemanticException
    {
        public IdentificatorNotFoundException(CToken token) : base(string.Format("Expected to find identificator, found {0}.", token)) { }
    }
    class IdentificatorAlreadyExistsException : SemanticException
    {
        public IdentificatorAlreadyExistsException(CIdentificator ident) : base(string.Format("Identificator '{0}' already exists.", ident.identName)) { }
    }

    class TypeNotFoundException : SemanticException
    {
        public TypeNotFoundException(string identName) : base(string.Format("Could not find type {0}.", identName)) { }
    }

    class InvalidExpressionException : SemanticException
    {
        public InvalidExpressionException(CToken token) : base(string.Format("Token {0} cannot be a part of expression.", token)) { }
    }
    class ExpectedNumberConstantException : SemanticException
    {
        public ExpectedNumberConstantException(EVarType t) : base(string.Format("Expected a number constant, got {0}.", t)) { }
    }
    class UnexpectedTokenException : SemanticException
    {
        public UnexpectedTokenException(CToken token) : base(string.Format("Expected end of program, got {0}.", token)) { }
    }
}
