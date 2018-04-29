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
            var test = new LocFileParser(System.IO.Path.Combine(approot, "Localizable.strings"));
            Console.ReadKey();
        }
    }
}
