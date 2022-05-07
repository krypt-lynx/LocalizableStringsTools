using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ExportStrings.Configuration
{
    static class XmlNodesEnumeration
    {
        static public IEnumerable<TNode> FilterNodes<TNode>(this XmlNodeList nodeList) where TNode : XmlNode
        {
            return nodeList
                .Cast<XmlNode>()
                .Where(x => x is TNode)
                .Cast<TNode>();
        }
    }

    class LocalizableGroup
    {
        public string Name;
        public string[] Paths;
        public string Mapping;

        public LocalizableGroup(XmlElement node)
        {
            Name = node.GetAttributeNode("name")?.Value;
            Paths = node.SelectNodes("path")
                .Cast<XmlNode>()
                .Select(x => x.InnerText)
                .ToArray();
            Mapping = node.GetAttributeNode("mapping")?.Value;
        } 
    }

    class LocaleMapping
    {
        public string Name;
        public (string, string)[] Mappings;

        public LocaleMapping(XmlElement node)
        {
            Name = node.GetAttributeNode("name")?.Value;
            Mappings = node.SelectNodes("map")
                .FilterNodes<XmlElement>()
                .Select(x => (x.GetAttributeNode("src")?.Value, x.GetAttributeNode("locale")?.Value))
                .ToArray();
        }
    }

    class Configuration
    {
        public Dictionary<string, LocalizableGroup> lprojects;
        public Dictionary<string, LocaleMapping> localeMappings;

        public Configuration(string path)
        {
            var xdoc = new XmlDocument();
            xdoc.Load(path);

            lprojects =
                xdoc.SelectNodes("project/localizables/LocalizableGroup")
                .FilterNodes<XmlElement>()
                .Select(x => new LocalizableGroup(x))
                .ToDictionary(x => x.Name);

            localeMappings =
                xdoc.SelectNodes("project/mappings/mapping")
                .FilterNodes<XmlElement>()
                .Select(x => new LocaleMapping(x))
                .ToDictionary(x => x.Name);
        }
    }
}
