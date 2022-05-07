using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.LocProject
{

    interface ILocaleMappable
    {
        Dictionary<string, string> Mapping { get; set; }
    }

    static class LocaleMappable
    {
        public static string MapLocale(this ILocaleMappable mappable, string locale)
        {
            if (mappable.Mapping.TryGetValue(locale, out string value))
            {
                return value;
            } 
            else
            {
                return locale;
            }
        }
    }
}
