using System;

namespace PaskalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleIO io = new ModuleIO("D:\\pascal\\helloWorld.pas");
            char c;
            try
            {
                while (true)
                {
                    c = io.NextChar();
                    Console.Write(c);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            }
        }
    }
}
