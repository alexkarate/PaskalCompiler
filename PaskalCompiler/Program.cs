using System;

namespace PaskalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleSyntax compiler = new ModuleSyntax("D:\\pascal\\error.pas");

            Console.WriteLine(compiler.GenerateListing());
        }
    }
}
