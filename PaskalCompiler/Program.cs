using System;

namespace PaskalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleIO io = new ModuleIO("D:\\pascal\\helloWorld.pas");
            ModuleLexical lexical = new ModuleLexical(io);
            CToken sym;
            while ((sym = lexical.NextSym())._tt != ETokenType.None)
            {
                Console.WriteLine(sym);
            }
            Console.WriteLine();
            
            for(int i = 0; i < io.Errors.Count; i++)
            {
                Console.WriteLine("Error: {0}. Line: {1}, Char: {2}", io.Errors[i].info.Message, io.Errors[i].lineNum, io.Errors[i].charNum);
            }
        }
    }
}
