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
        public ModuleSyntax(ModuleLexical lexer)
        {
            this.lexer = lexer;
        }

        public ModuleSyntax(string filePath)
        {
            ModuleIO io = new ModuleIO(filePath);
            lexer = new ModuleLexical(io);
            curSymbol = lexer.NextSym();
        }

        void Accept(CToken token)
        {
            if (token.Equals(curSymbol))
            {
                curSymbol = lexer.NextSym();
            }
            else
            {
                lexer.io.RecordError(new ErrorInformation(String.Format("Incorrect symbol. Expected {0}, got {1}", token, curSymbol)));
                curSymbol = CToken.empty;
            }
        }
        COperation Oper(EOperator op)
        {
            return new COperation(op);
        }
        CIdentificator Ident()
        {
            return new CIdentificator(null);
        }

        CValue Value(EVarType type)
        {
            return new CValue(type, null);
        }

        void Program()
        {
            Accept(Oper(EOperator.programsy));
            Accept(Ident());
            if (curSymbol.Equals(Ident()))
            {
                Accept(Ident());
                while(curSymbol.Equals(Oper(EOperator.comma)))
                {
                    Accept(Oper(EOperator.comma));
                    Accept(Ident());
                }
            }
            Accept(Oper(EOperator.semicolon));
            Block();
        }
        void Block()
        {

        }
    }
}
