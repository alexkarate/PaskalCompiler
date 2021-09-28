using System;
using System.Collections.Generic;
using System.IO;

namespace PaskalCompiler
{
    class ModuleIO
    {
        StreamReader file;
        public ModuleIO(string filePath)
        {
            FileStream fs = File.OpenRead(filePath);
            if(fs != null)
            {
                file = new StreamReader(fs);
            }
        }
        ~ModuleIO()
        {
            if (file != null)
            {
                file.Close();
                file = null;
            }
        }

        CToken GetNextToken()
        {
            return new CToken();
        }
    }
    enum TokenType { variableLiter, constantLiter, reservedLiter}
    enum ReservedWords { programWord, constWord, varWord, beginWord, endWord, ifWord, thenWord, forWord, ofWord }
    class CToken
    {
        public TokenType _tt;
        public CToken()
        {
            _tt = TokenType.constantLiter;
        }
        public override string ToString() { return "Generic token"; }
    }

    class CConstant : CToken
    {
        public CConstant()
        {
            _tt = TokenType.constantLiter;
        }
        public override string ToString() { return "Constant"; }
    }
}
