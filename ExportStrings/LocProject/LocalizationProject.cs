using StringsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.LocProject
{
    public class LocalizationProject : ILocalizable, ILocaleMappable
    {
        public string Path { get; private set; }
        public StringsFile[] Files { get; private set; }

        public string BaseLocale { get; private set; }

        public ILocalizable BaseFile { 
            get
            {
                return Files.FirstOrDefault(x => x.Locale == BaseLocale); // todo: performnce
            } 
        }

        public LocalizationProject(string path, IEnumerable<StringsFile> files, string baseLocale = Constants.LPROJ_BASE_LOCALE_NAME)
        {
            Path = path;
            Files = files.ToArray();
            BaseLocale = baseLocale;
        }

        public override string ToString()
        {
            return $"Project: {Path}; {Files?.Length ?? 0} file(s)";
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
