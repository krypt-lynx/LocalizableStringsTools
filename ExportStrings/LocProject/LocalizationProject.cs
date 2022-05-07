using StringsCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.LocProject
{
    public class LocalizationProject : ILocalizable, ILocaleMappable
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public StringsFile[] Files { get; private set; }

        public string BaseLocale { get; private set; }

        public ILocalizable BaseFile { 
            get
            {
                return Files.FirstOrDefault(x => x.Locale == BaseLocale); // todo: performnce
            } 
        }

        public LocalizationProject(string path, string name, string baseLocale = Constants.LPROJ_BASE_LOCALE_NAME)
        {
            Path = path;
            Name = name;
            BaseLocale = baseLocale;

            LoadFiles();
        }

        private void LoadFiles()
        {
            var dir = new DirectoryInfo(Path);
            List<StringsFile> files = new List<StringsFile>();
            foreach (var lproj in dir.GetDirectories("*" + Constants.LPROJ_EXTENSION))
            {
                var strings = lproj.GetFiles(Constants.STRINGS_SEARCH_FILTER);
                if (strings.Length == 0)
                {
                    continue;
                }

                if (strings.Length > 1)
                {
                    throw new Exception($"more then one .strings-file at path {lproj.FullName}");
                }

                string locale = lproj.Name.Substring(0, lproj.Name.Length - Constants.LPROJ_EXTENSION.Length);
                files.Add(new StringsFile(strings.First().FullName, locale));
            }

            Files = files.ToArray();
        }

        public override string ToString()
        {
            return $"Project: {Name}; Path: {Path}; {Files?.Length ?? 0} file(s)";
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return Files.Select(x => x.Keys).Aggregate(new SortedSet<string>(), (acc, keys) =>
                {
                    acc.UnionWith(keys);
                    return acc;
                });
            }
        }

        public IEnumerable<string> Locales
        {
            get
            {
                return Files.Select(x => x.Locale);
            }
        }

        public Dictionary<string, string> Mapping { get; set; }

        public string GetBaseValue(string key)
        {
            return BaseFile?.GetBaseValue(key);
        }

        public string GetValue(string key, string locale)
        {
            var file = Files.FirstOrDefault(x => x.Locale == locale); // todo: performnce 
            if (file != null)
            {
                file.GetValue(key);
            }
            return null;
        }

        public LocalizedValues GetValues(string key)
        {
            return new LocalizedValues
            {
                Key = key,
                Values = Files.Select(x => new LocalizedValue
                {
                    Locale = x.Locale,
                    Value = x.GetValue(key)
                }).ToArray()
            };
        }
    }
}
