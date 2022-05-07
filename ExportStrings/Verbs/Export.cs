using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ExportStrings.Verbs
{
    [Verb("export", HelpText = "export translation to csv table")]
    public class Export
    {
        [Value(0, HelpText = "Input file or directoty")]
        public IEnumerable<string> Input { get; set; }

        [Option("output", Required = true, HelpText = "Path to mod directory")]
        public string Output { get; set; }

        [Option("monolith", Default = false, HelpText = "Output as single file")]
        public bool SingleFile { get; set; }

        [Option("keysonly", Default = false, HelpText = "export keys only")]
        public bool KeysOnly { get; set; }

        [Option("header", Default = true, HelpText = "insert table header")]
        public bool Header { get; set; }
    }
}
