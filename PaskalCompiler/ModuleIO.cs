using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        public void RecordError(string error)
        {
            Errors.Add(new Error(new ErrorInformation(error), lineCounter, charCounter));
        }
        public void RecordError(ErrorInformation errorInfo) 
        {
            Errors.Add(new Error(errorInfo, lineCounter, charCounter));
        }
        public void RecordError(Exception e)
        {
            Errors.Add(new Error(new ErrorInformation(e.Message), lineCounter, charCounter));
        }

        public string GenerateListing()
        {
            int lineCount = 1, nextErrorId = -1;
            if (Errors.Count != 0)
                nextErrorId = 0;
            if (file != null)
            {
                file.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(file);
                StringBuilder listing = new StringBuilder();
                while(!reader.EndOfStream)
                {
                    listing.AppendLine(string.Format("{0, 4} {1}", lineCount, reader.ReadLine()));
                    while(nextErrorId != -1 && Errors[nextErrorId].lineNum == lineCount)
                    {
                        listing.AppendLine(string.Format("Error {0}: {1} Line {2}, Character {3}.", nextErrorId + 1, Errors[nextErrorId].info.Message, lineCount, Errors[nextErrorId].charNum));
                        nextErrorId++;
                        if (nextErrorId == Errors.Count)
                            nextErrorId = -1;
                    }
                    lineCount++;
                }
                return listing.ToString();
            }
            else
                return string.Empty;
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