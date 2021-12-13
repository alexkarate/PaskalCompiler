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

            AddIdentifier("false", IdentUseType.iConst, boolType);
            AddIdentifier("False", IdentUseType.iConst, boolType);
            AddIdentifier("true", IdentUseType.iConst, boolType);
            AddIdentifier("True", IdentUseType.iConst, boolType);

            AddIdentifier("writeln", IdentUseType.iFunc, voidType);
        }

        CIdentInfo AddIdentifier(string name, IdentUseType useType, CType type)
        {
            return scopes.Last().AddIdentifier(name, useType, type);
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

        const string ext = ".dll";

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
            methodILGenerator.Emit(OpCodes.Nop);
            methodILGenerator.EmitWriteLine("Testing...");
            methodILGenerator.Emit(OpCodes.Ret);
            
        }

        void OutputProgram()
        {
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
        void EmitMultiplicative(EOperator op)
        {
            switch (op)
            {
                case EOperator.star:
                    methodILGenerator.Emit(OpCodes.Mul);
                    break;
                case EOperator.slash:
                    methodILGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToSingle"));
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

        public void CheckProgram()
        {
            GenerateDefaultScope();
            CreateTokenLists();
            CreateGenerator();
            
            try
            {
                Program();
            }
            catch(CompilerException e)
            {
                IO.RecordError(e.Message);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (!curSymbol._tt.Equals(ETokenType.None))
                IO.RecordError("More tokens than expected.");
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
                        skip = false;
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
                IO.RecordError(e.Message);
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
                IO.RecordError(e.Message);
                SkipUntilToken(new CToken[] { Oper(EOperator.beginsy), Oper(EOperator.dot) });
            }
            //Functions();
            try
            {
                Operators();
            }
            catch(CompilerException e)
            {
                IO.RecordError(e.Message);
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
                    IO.RecordError(new UndeclaredIdentificatorException(curSymbol as CIdentificator).Message);
                    ident = AddIdentifier((curSymbol as CIdentificator).identName, IdentUseType.iVar, unknownType);
                }
                else if(ident.useType == IdentUseType.iFunc && ident.name == "writeln")
                {
                    Accept(Ident());
                    Accept(Oper(EOperator.openBr));
                    Expression();
                    Accept(Oper(EOperator.closeBr));
                    return;
                }
                else if (ident.useType != IdentUseType.iVar)
                    IO.RecordError(new VariableNotFoundException(ident).Message);

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
                        IO.RecordError(new UnderivableTypeException(l, r).Message);
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
                IO.RecordError(e.Message);
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
                IO.RecordError(e.Message);
                SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
            }
            if(!curSymbol.Equals(Oper(EOperator.semicolon)) && !curSymbol.Equals(Oper(EOperator.endsy)))
            {
                IO.RecordError(new InvalidSymbolException(Oper(EOperator.semicolon), curSymbol).Message);
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
                    IO.RecordError(e.Message);
                    SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
                }
                if(!curSymbol.Equals(Oper(EOperator.semicolon)) && !curSymbol.Equals(Oper(EOperator.endsy)))
                {
                    IO.RecordError(new InvalidSymbolException(Oper(EOperator.semicolon), curSymbol).Message);
                    SkipUntilToken(new CToken[] { Oper(EOperator.semicolon), Oper(EOperator.endsy) });
                }
            }
            Accept(Oper(EOperator.endsy));
        }
        void ChooseOperator()
        {
            Accept(Oper(EOperator.ifsy));
            CType t = Expression();
            if (t != unknownType && !t.isDerivedTo(boolType))
                IO.RecordError(new UnderivableTypeException(boolType, t).Message);
            try
            {
                Accept(Oper(EOperator.thensy));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e.Message);
                SkipUntilToken(operOrEndList);
            }
            Operator();
            if(curSymbol.Equals(Oper(EOperator.elsesy)))
            {
                Accept(Oper(EOperator.elsesy));
                Operator();
            }
        }

        void PreLoopOperator()
        {
            Accept(Oper(EOperator.whilesy));
            CType t = Expression();
            if (t != unknownType && !t.isDerivedTo(boolType))
                IO.RecordError(new UnderivableTypeException(boolType, t).Message);
            try
            {
                Accept(Oper(EOperator.dosy));
            }
            catch(CompilerException e)
            {
                IO.RecordError(e.Message);
                SkipUntilToken(operOrEndList);
            }
            Operator();
        }

        CType Expression()
        {
            CType l = SimpleExpression(), r = unknownType;
            COperation oper = curSymbol as COperation;
            if (oper != null && oper.IsRelative())
            {
                Accept(Oper(oper._vo));
                r = SimpleExpression();
                if (l != unknownType && r != unknownType && !r.isDerivedTo(l) && !l.isDerivedTo(r))
                    IO.RecordError(new UnderivableTypeException(l, r, oper).Message);
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
                    if (l.isDerivedTo(r))
                        l = r;
                    else if (!r.isDerivedTo(l))
                        IO.RecordError (new UnderivableTypeException(l, r, oper).Message);
                }
                EmitAdditive(oper._vo);
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
                        IO.RecordError(new UnderivableTypeException(l, r, oper).Message);
                }
                EmitMultiplicative(oper._vo);
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
                    IO.RecordError(new UndeclaredIdentificatorException(curSymbol as CIdentificator).Message);
                    ident = AddIdentifier((curSymbol as CIdentificator).identName, IdentUseType.iVar, unknownType);
                }
                else if (ident.useType == IdentUseType.iClass)
                    IO.RecordError(new VariableNotFoundException(ident).Message);
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
                    IO.RecordError(e.Message);
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
                IO.RecordError(new InvalidExpressionException(curSymbol).Message);
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
                    methodILGenerator.Emit(OpCodes.Ldind_U2, Convert.ToUInt16((char)c.value));
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
                    methodILGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32((int)c.value));
                    Accept(Value(EVarType.vtInt));
                    return intType;
                case EVarType.vtReal:
                    methodILGenerator.Emit(OpCodes.Ldc_R4, Convert.ToInt32((float)c.value));
                    Accept(Value(EVarType.vtReal));
                    return realType;
                default:
                    IO.RecordError(new ExpectedNumberConstantException(c._vt).Message);
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
                IO.RecordError(e.Message);
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
                        IO.RecordError(new IdentificatorNotFoundException(varList[i]).Message);
                    else
                        AddIdentifier(identificator.identName, IdentUseType.iVar, type);
                }
            }
            catch(CompilerException e)
            {
                IO.RecordError(e.Message);
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
                IO.RecordError(new TypeNotFoundException(name).Message);
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
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_integer || b._tt == EType.et_real || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (!isDerivedTo(b))
                return false;
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
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_real || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (!isDerivedTo(b))
                return false;
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
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_boolean || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (!isDerivedTo(b))
                return false;
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
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_char || b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (!isDerivedTo(b))
                return false;
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
        }
        public override bool isDerivedTo(CType b)
        {
            if (b._tt == EType.et_string)
                return true;
            return false;
        }
        public override bool isDerivedTo(CType b, COperation op)
        {
            if (!isDerivedTo(b))
                return false;
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
        public CIdentInfo(IdentUseType useType, CType type, string name)
        {
            this.useType = useType;
            this.type = type;
            this.name = name;
        }
    }

    class Scope
    {
        //public Dictionary<string, Name?> 
        //public Dictionary<Name?, CIdentInfo> identifierTable;
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

        public CIdentInfo AddIdentifier(string ident, IdentUseType useType, CType type)
        {
            CIdentInfo value = new CIdentInfo(useType, type, ident);
            identifierTable.Add(ident, value);
            return value;
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
        public UnderivableTypeException(CType l, CType r, COperation op) : base(string.Format("{0} cannot be applied to {1} and {2}", op, l._tt, r._tt)) { }
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
        public ExpectedNumberConstantException(EVarType t) : base(string.Format("Expected a number constant, got {0}", t)) { }
    }
}
