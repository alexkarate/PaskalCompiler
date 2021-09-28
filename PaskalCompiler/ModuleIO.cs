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
    enum TokenType { variable, something, el }
    class CToken
    {
        TokenType _tt;
        public override string ToString() { return "Generic token"; }
    }
}
