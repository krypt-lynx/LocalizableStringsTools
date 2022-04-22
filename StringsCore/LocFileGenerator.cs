using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StringsCore
{
    public class LocFileGenerator
    {
        private LocFile document;

        public LocFileGenerator(LocFile document)
        {
            this.document = document;
        }

        public void Write(string path)
        {
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            TextWriter writer = new StreamWriter(stream);
            this.Write(writer);
            writer.Close();
            stream.Close();
            writer.Dispose();
            stream.Dispose();
        }

        public void Write(TextWriter writer)
        {
            WriteRecursive(writer, document);
        }


        public void WriteRecursive(TextWriter writer, LocTreeEntry treeEntry)
        {
            foreach (LocEntry entry in treeEntry.entries)
            {
                LocTextEntry textEntry = (entry as LocTextEntry);
                switch (entry.Type)
                {
                    case LocEntry.EntryType.Document:
                    case LocEntry.EntryType.LocPair:
                        this.WriteRecursive(writer, entry as LocTreeEntry);
                        break;
                    case LocEntry.EntryType.BlockComment:
                        {
                            writer.Write("/*");
                            writer.Write(textEntry.Text);
                            writer.Write("*/");
                        }
                        break;
                    case LocEntry.EntryType.LineComment:
                        {
                            writer.Write("//");
                            writer.Write(textEntry.Text);
                            writer.Write("\n");
                        }
                        break;
                    case LocEntry.EntryType.Text:
                        {
                            writer.Write(textEntry.Text);
                        }
                        break;
                    case LocEntry.EntryType.Key:
                    case LocEntry.EntryType.Value:
                        {
                            writer.Write("\"");
                            writer.Write(FormatString(textEntry.Text));
                            writer.Write("\"");
                        }
                        break;
                    case LocEntry.EntryType.Separator:
                    case LocEntry.EntryType.Semicolon:
                    case LocEntry.EntryType.NewLine:
                        {
                            writer.Write(textEntry.Text);
                        }
                        break;
                    default:
                        {
                            throw new InvalidOperationException(string.Format("Unexpected type: {0}", entry.Type));
                        }
                }
            }
            writer.Flush();
            (writer as StreamWriter).BaseStream.Flush();
        }

        public string FormatString(string str)
        {
            Dictionary<char, string> escaped = new Dictionary<char, string>()
            {
                { '\\', "\\\\" },
                { '\"', "\\\"" },
                { '\r', "\\r" },
                { '\n', "\\n" },
                { '\t', "\\t" },
            };

            StringBuilder sb = new StringBuilder();
            foreach (char ch in str)
            { 
                if (escaped.ContainsKey(ch))
                {
                    sb.Append(escaped[ch]);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString(); 
        }
    }
}
