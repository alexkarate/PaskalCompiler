﻿using System;

namespace PaskalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleSyntax compiler = new ModuleSyntax("D:\\pascal\\generator4.pas");
            compiler.CompileProgram();
            Console.WriteLine(compiler.GenerateListing());
        }
    }
}
