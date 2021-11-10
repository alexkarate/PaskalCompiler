using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaskalCompiler
{
    class ModuleSyntax
    {
        ModuleLexical lexer;
        CToken curSymbol;
        bool skipToNext = false;
        bool analyzed = false;
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
                skipToNext = false;
            }
            else
            {
                if (!skipToNext)
                {
                    lexer.io.RecordError(new ErrorInformation(String.Format("Incorrect symbol. Expected {0}, got {1}", token, curSymbol)));
                    skipToNext = true;
                }
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
        public void CheckProgram()
        {
            Program();
            if (curSymbol._tt != ETokenType.None)
                lexer.io.RecordError(new ErrorInformation("More tokens than expected"));
            analyzed = true;
        }
        void Program()
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
                Accept(Ident());
                Accept(Oper(EOperator.assignSy));
                Expression();
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
            Expression();
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
            Expression();
            Accept(Oper(EOperator.dosy));
            Operator();
        }

        void Expression()
        {
            SimpleExpression();
            COperation oper = curSymbol as COperation;
            if (oper != null && oper.IsRelative())
            {
                Accept(Oper(oper._vo));
                SimpleExpression();
            }
        }
        void SimpleExpression()
        {
            Sign();
            Term();
            COperation oper = curSymbol as COperation;
            while (oper != null && oper.IsAdditive())
            {
                Accept(Oper(oper._vo));
                Term();
                oper = curSymbol as COperation;
            }
        }

        void Term()
        {
            Factor();
            COperation oper = curSymbol as COperation;
            while(oper != null && oper.IsMultiplicative())
            {
                Accept(Oper(oper._vo));
                Factor();
                oper = curSymbol as COperation;
            }
        }

        void Factor()
        {
            if (curSymbol.Equals(Ident()))
                Accept(Ident());
            else if (curSymbol.Equals(Oper(EOperator.openBr)))
            {
                Accept(Oper(EOperator.openBr));
                Expression();
                Accept(Oper(EOperator.closeBr));
            }
            else
                ConstNoSign();
        }

        void ConstNoSign()
        {
            if (curSymbol.Equals(Value(EVarType.vtString)))
                Accept(Value(EVarType.vtString));
            else if (curSymbol.Equals(Value(EVarType.vtChar)))
                Accept(Value(EVarType.vtChar));
            else if (curSymbol.Equals(Value(EVarType.vtBoolean)))
                Accept(Value(EVarType.vtBoolean));
            else
                NumberNoSign();
        }
        void NumberNoSign()
        {
            if (curSymbol.Equals(Value(EVarType.vtReal)))
                Accept(Value(EVarType.vtReal));
            else
                Accept(Value(EVarType.vtInt));
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
            Accept(Ident());
            while(curSymbol.Equals(Oper(EOperator.comma)))
            {
                Accept(Oper(EOperator.comma));
                Accept(Ident());
            }
            Accept(Oper(EOperator.colon));
            SingleType();
        }
        void SingleType()
        {
            SimpleType();
        }
        void SimpleType()
        {
            Accept(Ident());
        }
    }
}
