using System;
using System.Collections.Generic;
using System.IO;

namespace PaskalCompiler
{
    class ModuleIO
    {
        FileStream file;
        byte[] buffer;
        int bufferCounter;
        long charCounter;
        long lineCounter;
        int bufferLength;
        const int readCount = 1028;

        public ModuleIO(string filePath)
        {
            file = File.OpenRead(filePath);
            bufferCounter = readCount;
            bufferLength = readCount;
            charCounter = 0;
            lineCounter = 0;
            buffer = new byte[readCount];
        }
        ~ModuleIO()
        {
            if (file != null)
            {
                file.Close();
                file = null;
            }
        }

        public char NextChar()
        {
            if(bufferCounter >= bufferLength)
            {
                int bufferLength = file.Read(buffer, 0, readCount);
                if (bufferLength == 0)
                    throw new Exception("EOF");
                bufferCounter = 0;
            }
            char nextChar = (char)buffer[bufferCounter++];
            charCounter++;
            if (nextChar == '\n')
                lineCounter++;
            return nextChar;
        }
        /*
        CToken GetNextToken()
        {
            
            return new CToken();
        }
        */
    }
    /*
    enum TokenType { identifierLiter, constantLiter, keywordLiter, separatorLiter, operatorLiter, commentLiter}
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
    */

}
/*
 * char nextch()
 * {
 *      if(buffer is empty) {read next buffer;}
 *      inc counter;
 *      return current liter;
 * }
 * 
 * error(error information){
 *      record error to buffer
 *      (position in code)
 *      (code connected to error)
 * }
 */