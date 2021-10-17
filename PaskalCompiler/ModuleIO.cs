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

        public List<Error> Errors { get; }
        public ModuleIO(string filePath)
        {
            file = File.OpenRead(filePath);
            bufferCounter = 0;
            bufferLength = 0;
            charCounter = 0;
            lineCounter = 1;
            buffer = new byte[readCount];
            Errors = new List<Error>();
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
            char c = '\0';
            if (bufferCounter >= bufferLength)
            {
                bufferLength = file.Read(buffer, 0, readCount);
                if (bufferLength == 0)
                    return c;
                bufferCounter = 0;
            }
            c = (char)buffer[bufferCounter++];

            if (c == '\n')
            {
                lineCounter++;
                charCounter = 1;
            }
            else
                charCounter++;
            return c;
        }

        public void RecordError(ErrorInformation errorInfo) 
        {
            Errors.Add(new Error(errorInfo, lineCounter, charCounter));
        }
    }

    class Error
    {
        public ErrorInformation info;
        public long lineNum;
        public long charNum;
        public Error(ErrorInformation info, long lineNum, long charNum)
        {
            this.lineNum = lineNum;
            this.charNum = charNum;
            this.info = info;
        }
    }
    class ErrorInformation
    {
        public string Message { get; }
        public ErrorInformation(string message)
        {
            Message = message;
        }
    }
}