using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using strings2csv;
using StringsCore;
using System.IO;

namespace ExportStrings
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: exportstrings <input file> <output file>");
                Environment.Exit(1);
            }


            string approot = System.AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                var loc = new LocFile(System.IO.Path.Combine(approot, args[0]));

                StreamWriter csv = new StreamWriter(System.IO.Path.Combine(approot, args[1]));
                CSV.Write(csv, loc.localizationPairs.Select(x => new List<string>(new string[] { x.Key, x.Value })).ToList());
                csv.Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unexpected exception: {0}", e.Message);
                Environment.Exit(2);
            }
        }
    }
}
