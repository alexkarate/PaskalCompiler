using System;
using System.Collections.Generic;
using System.Linq;

namespace PaskalCompiler
{
    
    class ModuleSyntax
    {
        ModuleLexical lexer;
        CToken curSymbol;
        bool skipToNext = false;
        bool analyzed = false;

        List<Scope> scopes;
        CType intType, realType, boolType, charType, stringType;
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
                throw new InvalidSymbolException(curSymbol, token);
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

        void GenerateDefaultContext()
        {
            scopes = new List<Scope>();
            PushScope();

            intType = AddType(EType.et_integer);
            realType = AddType(EType.et_real);
            boolType = AddType(EType.et_boolean);
            charType = AddType(EType.et_char);
            stringType = AddType(EType.et_string);

            AddIdentifier("integer", IdentUseType.iClass, intType);
            AddIdentifier("real", IdentUseType.iClass, realType);
            AddIdentifier("boolean", IdentUseType.iClass, boolType);
            AddIdentifier("char", IdentUseType.iClass, charType);
            AddIdentifier("string", IdentUseType.iClass, stringType);

            AddIdentifier("false", IdentUseType.iConst, boolType);
            AddIdentifier("False", IdentUseType.iConst, boolType);
            AddIdentifier("true", IdentUseType.iConst, boolType);
            AddIdentifier("True", IdentUseType.iConst, boolType);
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


        public void CheckProgram()
        {
            GenerateDefaultContext();
            try
            {
                Program();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (curSymbol._tt != ETokenType.None)
                lexer.io.RecordError(new ErrorInformation("More tokens than expected"));
            analyzed = true;
        }
        void Program()
        {
            PushScope();
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
            Block();
            Accept(Oper(EOperator.dot));
        }
        void Block()
        {
            //Labels();
            //Constants();
            //Types();
            Variables();
            //Functions();
            Operators();
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
                    throw new UndeclaredIdentificatorException(curSymbol as CIdentificator);
                else if (ident.useType != IdentUseType.iVar)
                    throw new VariableNotFoundException(ident);

                CType l = ident.type;
                Accept(Ident());
                Accept(Oper(EOperator.assignSy));
                CType r = Expression();
                if (!r.isDerivedTo(l))
                    throw new UnderivableTypeException(l, r);
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
            Accept(Oper(EOperator.beginsy));
            Operator();
            while(curSymbol.Equals(Oper(EOperator.semicolon)))
            {
                Accept(Oper(EOperator.semicolon));
                Operator();
            }
            Accept(Oper(EOperator.endsy));
        }
        void ChooseOperator()
        {
            Accept(Oper(EOperator.ifsy));
            CType t = Expression();
            if (!t.isDerivedTo(boolType))
                throw new UnderivableTypeException(boolType, t);
            Accept(Oper(EOperator.thensy));
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
            if (!t.isDerivedTo(boolType))
                throw new UnderivableTypeException(boolType, t);
            Accept(Oper(EOperator.dosy));
            Operator();
        }

        CType Expression()
        {
            CType l = SimpleExpression();
            COperation oper = curSymbol as COperation;
            if (oper != null && oper.IsRelative())
            {
                Accept(Oper(oper._vo));
                CType r = SimpleExpression();
                if (!r.isDerivedTo(l) && !l.isDerivedTo(r))
                    throw new UnderivableTypeException(l, r);
                l = boolType;
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
                if (l.isDerivedTo(r))
                    l = r;
                else if (!r.isDerivedTo(l))
                    throw new UnderivableTypeException(l, r);
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
                if (l.isDerivedTo(r))
                    l = r;
                else if (!r.isDerivedTo(l))
                    throw new UnderivableTypeException(l, r);
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
                    throw new IdentificatorNotFoundException(curSymbol);
                else if (ident.useType == IdentUseType.iClass)
                    throw new VariableNotFoundException(ident);
                Accept(Ident());
                return ident.type;
            }
            else if (curSymbol.Equals(Oper(EOperator.openBr)))
            {
                CType t;
                Accept(Oper(EOperator.openBr));
                t = Expression();
                Accept(Oper(EOperator.closeBr));
                return t;
            }
            else
                return ConstNoSign();
                
        }

        CType ConstNoSign()
        {
            if (curSymbol.Equals(Value(EVarType.vtString)))
            {
                Accept(Value(EVarType.vtString));
                return stringType;
            }
            else if (curSymbol.Equals(Value(EVarType.vtChar)))
            {
                Accept(Value(EVarType.vtChar));
                return charType;
            }
            else if (curSymbol.Equals(Value(EVarType.vtBoolean)))
            {
                Accept(Value(EVarType.vtBoolean));
                return boolType;
            }
            else
                return NumberNoSign();
        }
        CType NumberNoSign()
        {
            if (curSymbol.Equals(Value(EVarType.vtReal)))
            {
                Accept(Value(EVarType.vtReal));
                return realType;
            }
            else
            {
                Accept(Value(EVarType.vtInt));
                return intType;
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
            Accept(Ident());
            while(curSymbol.Equals(Oper(EOperator.comma)))
            {
                Accept(Oper(EOperator.comma));
                varList.Add(curSymbol);
                Accept(Ident());
            }
            Accept(Oper(EOperator.colon));
            var type = SingleType();
            CIdentificator identificator;
            for(int i = 0; i < varList.Count; i++)
            {
                identificator = varList[i] as CIdentificator;
                if (identificator == null)
                    throw new IdentificatorNotFoundException(varList[i]);
                AddIdentifier(identificator.identName, IdentUseType.iVar, type);
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
                throw new ClassNotFoundException(ident.name);
            
            return ident.type;
        }
        
    }
    class SyntaxException : ApplicationException
    {
        public SyntaxException() : base("Generic syntax exception.") { }
        public SyntaxException(string s) : base(s) { }
    }
    class SemanticException : ApplicationException
    {
        public SemanticException() : base("Generic semantic exception.") { }
        public SemanticException(string s) : base(s) { }
    }
    class UnderivableTypeException : SemanticException
    {
        public UnderivableTypeException(CType l, CType r) : base(string.Format("Type {0} is not derivable to {1}", r._tt, l._tt)) { }
    }
    class UndeclaredIdentificatorException : SemanticException
    {
        public UndeclaredIdentificatorException(CIdentificator ident) : base(string.Format("Identificator {0} not found", ident.identName)) { }
    }

    class VariableNotFoundException : SemanticException
    {
        public VariableNotFoundException(CIdentInfo info) : base(string.Format("Expected to find variable, found {0}", info.type)) { }
    }

    class IdentificatorNotFoundException : SemanticException
    {
        public IdentificatorNotFoundException(CToken token) : base(string.Format("Expected to find identificator, found {0}", token)) { }
    }

    class ClassNotFoundException : SemanticException
    {
        public ClassNotFoundException(string identName) : base(string.Format("Could not find class {0}", identName)) { }
    }

    class InvalidSymbolException : SemanticException
    {
        public CToken expected, current;
        public InvalidSymbolException(CToken expected, CToken current) : base(string.Format("Expected {0}, got {1}.", expected, current))
        {
            this.expected = expected;
            this.current = current;
        }
    }

    enum EType
    {
        et_integer,
        et_real,
        et_boolean,
        et_char,
        et_string
    }

    abstract class CType
    {
        public abstract bool isDerivedTo(CType b);
        public EType _tt;
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
    }

    enum IdentUseType
    {
        iVar,
        iClass,
        iConst
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
                default:
                    return null;
            }
            typeTable.Add(newType);
            return newType;
        }
    }
}
