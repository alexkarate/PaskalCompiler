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

        void Program()
        {
            Accept(new COperation(EOperator.programsy));
            Accept(new CIdentificator(null));
            if (curSymbol.Equals(new CIdentificator(null)))
            {
                Accept(new CIdentificator(null));
                while(curSymbol.Equals(new COperation(EOperator.comma)))
                {
                    Accept(new COperation(EOperator.comma));
                    Accept(new CIdentificator(null));
                }
            }
            Accept(new COperation(EOperator.semicolon));
            Block();
        }
        void Block()
        {

        }
    }
}
