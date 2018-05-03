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
        static string approot = AppDomain.CurrentDomain.BaseDirectory;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("usage: exportstrings <input file> <output file>");
                Environment.Exit(1);
            }

          /*  try
            {*/
                switch (args[0].ToLowerInvariant())
                {
                    case "export":
                        Export(args[1], args[2]);
                        break;
                    case "update":
                        Update(args[1], args[2], args[3], args[4]);
                        break;
                    case "generate":
                        Generate(args[1], args[2]);
                        break;
                }
          /*  }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unexpected exception: {0}", e.Message);
                Environment.Exit(2);
            }*/


        }

        private static void Generate(string data, string dest)
        {
            StreamReader stream = new StreamReader(Path.Combine(approot, data));
            var csv = CSV.ToList(stream).ToDictionary(x => x[0], x => x[1]);

            LocFile doc = new LocFile();

            foreach (var kvp in csv)
            {
                doc.Append(CreateLocPair(kvp.Key, kvp.Value));
                doc.Append(CreateNewLine());

            }

            var gen = new LocFileGenerator(doc);
            gen.Write(Path.Combine(approot, dest));
        }

        enum UpdateStrategy
        {
            ReplaceOnly,
            ReplaceAndAppend,
            AppendOnly
        }

        private static void Update(string strategy, string tepmlate, string data, string dest)
        {
            UpdateStrategy updateStrategy;
            Enum.TryParse(strategy, true, out updateStrategy);

            var loc = new LocFileParser(Path.Combine(approot, tepmlate));
            var doc = loc.Parse();

            StreamReader stream = new StreamReader(Path.Combine(approot, data));
            var csv = CSV.ToList(stream);//

            switch (updateStrategy)
            {
                case UpdateStrategy.AppendOnly:
                    throw new NotImplementedException();
                    //break;
                case UpdateStrategy.ReplaceAndAppend:
                    ReplaceAppend(doc, csv);
                    break;
                case UpdateStrategy.ReplaceOnly:
                    UpdateReplace(doc, csv.ToDictionary(x => x[0], x => x[1]));
                    break;
            }

            var gen = new LocFileGenerator(doc);
            gen.Write(Path.Combine(approot, dest));
        }

        private static void ReplaceAppend(LocFile doc, List<List<string>> csv)
        {
            var entryList = new LinkedList<LocEntry>(doc.entries);


            List<LinkedListNode<LocEntry>> nodes = new List<LinkedListNode<LocEntry>>();
            var node = entryList.First;
            while (node != null)
            {
                nodes.Add(node);
                node = node.Next;
            }
            var locNodes = nodes.Where(x => x.Value.Type == LocEntry.EntryType.LocPair).ToDictionary(x => (x.Value as LocPairBlock).Key, x => x);

            LinkedListNode<LocEntry> lastKnownNode = null;
            foreach (var line in csv)
            {
                var key = line[0];
                var value = line[1];
                
                // Если есть ключ - просто замегяем значение
                if (locNodes.ContainsKey(key))
                {
                    lastKnownNode = locNodes[key];
                    (locNodes[key].Value as LocPairBlock).Value = value;
                }
                else
                {
                    // Иначе пытаемся вставить новое значение после последнего обработанного ключа
                    if (lastKnownNode != null)
                    {
                        var newPair = lastKnownNode.Value.Clone() as LocPairBlock;
                        newPair.Key = key;
                        newPair.Value = value;

                        // Пытаемся вставить перенос строки
                        var newLineBlock = FindNextLine(lastKnownNode);
                        if (newLineBlock != null)
                        {
                            switch (newLineBlock.Value.Type)
                            {
                                case LocEntry.EntryType.Text:
                                    {
                                        SplitTextNode(entryList, newLineBlock, "\n");
                                        lastKnownNode = entryList.AddAfter(newLineBlock, newPair);
                                        entryList.AddAfter(lastKnownNode, CreateNewLine());
                                    }
                                    break;
                                case LocEntry.EntryType.LineComment:
                                    {
                                        lastKnownNode = entryList.AddAfter(newLineBlock, newPair);
                                        entryList.AddAfter(lastKnownNode, CreateNewLine());
                                    }
                                    break;
                                default:
                                    throw new InvalidOperationException(string.Format("Unexpected block type: {0}", newLineBlock.Value.Type));
                            }
                        }
                        else
                        {
                            lastKnownNode = entryList.AddAfter(lastKnownNode, newPair);
                        }

                        locNodes[key] = lastKnownNode;
                    }
                    else
                    {
                        // ... Но это первый ключ, который мы пытаемся обработать
                        if (entryList.First != null)
                        {
                            // Если в файле вообще есть ключи - вставляем новый в самое начало копируя его фоматирование
                            var newPair = entryList.First.Value.Clone() as LocPairBlock; // new line
                            newPair.Key = newPair.Key;
                            newPair.Value = newPair.Value;

                            lastKnownNode = entryList.AddBefore(entryList.First, newPair);
                            entryList.AddAfter(lastKnownNode, CreateNewLine());
                            locNodes[key] = lastKnownNode;
                        }
                        else
                        {
                            // Или просто вставляем, если нет
                            LocPairBlock newPair = CreateLocPair(key, value);

                            lastKnownNode = entryList.AddLast(newPair);
                            entryList.AddAfter(lastKnownNode, CreateNewLine());
                            locNodes[key] = lastKnownNode;
                        }
                    }
                }
            }

            doc.entries.Clear();
            doc.entries.AddRange(entryList);
            //var locEntries = doc.localizableEntries.ToDictionary(x => x.Key, x => x);

        }

        private static void SplitTextNode(LinkedList<LocEntry> list, LinkedListNode<LocEntry> textNode, string separator)
        {
            var text = (textNode.Value as TextBlock).Text;
            var index = text.IndexOf(separator);

            if (index == -1)
            {
                return;
            }

            var p1 = text.Substring(0, index + separator.Length);
            var p2 = text.Substring(index + separator.Length);

            if (p2.Length == 0)
            {
                return;
            }

            (textNode.Value as TextBlock).Text = p1;
            list.AddAfter(textNode, new TextBlock(p2));
        }

        private static LinkedListNode<LocEntry> FindNextLine(LinkedListNode<LocEntry> lastKnownNode)
        {
            var node = lastKnownNode.Next;
            LinkedListNode<LocEntry> lastLineComment = null;
            while (node != null) 
            {
                if ((node.Value is TextBlock) &&
                    (node.Value as TextBlock).Text.Contains('\n'))
                {
                    return node;
                }
                
                if (node.Value is LineCommentBlock)
                {
                    lastLineComment = node;
                }

                else if (node.Value is LocPairBlock)
                {
                    return lastLineComment;
                }
                node = node.Next;
            }

            return lastLineComment;
        }

        private static LocPairBlock CreateLocPair(string key, string value)
        {
            // "key = value;"

            var newPair = new LocPairBlock();

            newPair.Append(new KeyBlock(key));
            newPair.Append(new TextBlock(" "));
            newPair.Append(new SeparatorBlock());
            newPair.Append(new TextBlock(" "));
            newPair.Append(new ValueBlock(value));
            newPair.Append(new SemicolonBlock());

            return newPair;
        }

        private static TextBlock CreateNewLine()
        {
            return new TextBlock("\n");
        }

        private static void UpdateReplace(LocFile doc, Dictionary<string, string> csv)
        {
            var locEntries = doc.localizableEntries.ToList();
            int missedKeysCount = 0;
            HashSet<string> usedKeys = new HashSet<string>();

            foreach (var entry in locEntries)
            {
                if (csv.ContainsKey(entry.Key))
                {
                    entry.Value = csv[entry.Key];
                    usedKeys.Add(entry.Key);
                }
                else
                {
                    missedKeysCount++;
                }
            }

            Console.WriteLine("Missed keys: {0}", missedKeysCount);
            Console.WriteLine("Unused keys: {0}", locEntries.Count() - usedKeys.Count);
        }

        private static void Export(string src, string dest)
        {
            var loc = new LocFileParser(System.IO.Path.Combine(approot, src));
            var doc = loc.Parse();

            StreamWriter csv = new StreamWriter(System.IO.Path.Combine(approot, dest));
            CSV.Write(csv, doc.localizableEntries.Select(x => new List<string>(new string[] { x.Key, x.Value })).ToList());
            csv.Close();
        }
    }
}
