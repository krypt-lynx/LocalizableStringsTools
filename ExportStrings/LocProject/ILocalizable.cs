using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.LocProject
{
    public struct LocalizedValue
    {
        public string Locale;
        public string Value;

        public override string ToString()
        {
            return $"{{{Locale}: {Value}}}";
        }
    }

    public struct LocalizedValues
    {
        public string Key;
        public LocalizedValue[] Values;

        public override string ToString()
        {
            return $"{{Key: {Key}; {{{String.Join("; ", Values)}}} }}";
        }
    }

    public interface ILocalizable
    {
        IEnumerable<string> Keys { get; }
        IEnumerable<string> Locales { get; }
        string GetBaseValue(string key);
        string GetValue(string key, string locale);
        LocalizedValues GetValues(string key);
    }
}
