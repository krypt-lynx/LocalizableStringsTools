using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsCore
{
    class Program
    {
        static void Main(string[] args)
        {
            string approot = System.AppDomain.CurrentDomain.BaseDirectory;
            var test = new LocFile(System.IO.Path.Combine(approot, "Localizable.strings"));
            Console.ReadKey();
        }
    }
}
