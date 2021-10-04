using System;

namespace PaskalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleIO io = new ModuleIO("D:\\pascal\\helloWorld.pas");
            char c;
            int t = 0;
            while (io.NextChar(out c))
            {
                Console.Write(c);
                if (t++ % 12 == 0)
                    io.RecordError(new ErrorInformation("Test Error"));
            }
            Console.WriteLine();
            
            for(int i = 0; i < io.Errors.Count; i++)
            {
                Console.WriteLine("Error: {0}. Line: {1}, Char: {2}", io.Errors[i].info.Message, io.Errors[i].lineNum, io.Errors[i].charNum);
            }
            
        }
    }
}
