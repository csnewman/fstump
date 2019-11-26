using System;

namespace FStump
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FStump Compiler");

            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect args: FStump {src} {dst}");
                return;
            }
            
            var compiler = new FStumpCompiler();
            compiler.Compile(args[0], args[1]);
        }
    }
}
