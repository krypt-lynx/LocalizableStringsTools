using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StringsCore;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            string approot = System.AppDomain.CurrentDomain.BaseDirectory;
            var parser = new LocFileParser(System.IO.Path.Combine(approot, "Localizable.strings"));
            var document = parser.Parse();

            var generator = new LocFileGenerator(document);
            generator.Write(System.IO.Path.Combine(approot, "restored.strings"));

            //test.Save(System.IO.Path.Combine(approot, "Localizable_restored.strings"));
            Console.ReadKey();
        }
    }
}
