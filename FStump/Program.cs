using System;

namespace FStump
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            var compiler = new FStumpCompiler();
            compiler.Compile("../../../example.fss", "example.s");
        }
    }
}
