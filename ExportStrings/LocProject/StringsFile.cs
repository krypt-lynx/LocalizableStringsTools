using StringsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace ExportStrings.LocProject
{
    public class StringsFile: ILocalizable
    {
        public string Path { get; private set; }

        public string Locale { get; private set; }

        public string FileName { get; private set; }

        public LocFile semanticTree;
        public LocFile SemanticTree
        {
            get
            {
                if (semanticTree == null)
                {
                    semanticTree = LoadStrings();
                }

                return semanticTree;
            }
        }

        public StringsFile(string path, string locale)
        {

            Path = path;
            Locale = locale;

            ParsePath(path);
        }

        private void ParsePath(string path)
        {
            var components = path.Split(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar);

            FileName = components.Last();
        }

        private LocFile LoadStrings()
        {
            var parser = new LocFileParser(Path);
            return parser.Parse();
        }

        public string GetValue(string key)
        {
            return GetValueInternal(key);
        }


        private string GetValueInternal(string key)
        {
            if (semanticTree.localizableEntries.Contains(key))
            {
                return semanticTree.localizableEntries[key].Last().Value;
            }
            else
            {
                return null;
            }
        }

        public override string ToString()
        {
            return $"Locale: {Locale}; File: {FileName}";
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return SemanticTree.localizableEntries.Select(x => x.Key);
            }
        }

        public IEnumerable<string> Locales
        {
            get 
            {
                yield return Locale;
            }
        }

        public string GetBaseValue(string key)
        {
            return GetValueInternal(key);
        }

        public string GetValue(string key, string locale)
        {
            if (locale == Locale)
            {
                return GetValueInternal(key);
            }
            else
            {
                return null;
            }
        }

        public LocalizedValues GetValues(string key)
        {
            return new LocalizedValues
            {
                Key = key,
                Values = new LocalizedValue[] {
                    new LocalizedValue
                    {
                        Locale = Locale,
                        Value = GetValueInternal(key)
                    }
                }
            };
        }
    }
}
