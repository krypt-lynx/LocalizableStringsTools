using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsCore
{
    public class LocEntry
    {
        public enum EntryType
        {
            Text,
            BlockComment,
            LineComment,
            Separator,
            Semicolon,
            Key,
            Value,
            LocPair,
            Document
        }

        public EntryType type { get; private set; }

        public LocEntry(EntryType type)
        {
            this.type = type;
        }
    }

    public abstract class LocTextEntry : LocEntry
    {
        public string text;

        public LocTextEntry(string text, EntryType type) : base(type)
        {
            this.text = text;
        }
    }

    public abstract class LocTreeEntry : LocEntry
    {
        public List<LocEntry> entries = new List<LocEntry>();

        public LocTreeEntry(EntryType type) : base(type) { }

        public void Append(LocEntry entry)
        {
            entries.Add(entry);
        }

        internal virtual void FinalizeTree()
        {
            foreach (LocEntry entry in entries)
            {
                if (entry is LocTreeEntry)
                {
                    (entry as LocTreeEntry).FinalizeTree();
                }
            }
        }
    }

    public class TextBlock : LocTextEntry
    {
        public TextBlock(string text) : base(text, EntryType.Text) { }

        public override string ToString()
        {
            return String.Format("t: {0}", text);
        }
    }

    public class BlockCommentBlock : LocTextEntry
    {
        public BlockCommentBlock(string text) : base(text, EntryType.BlockComment) { }

        public override string ToString()
        {
            return String.Format("bc: {0}", text);
        }
    }

    public class LineCommentBlock : LocTextEntry
    {
        public LineCommentBlock(string text) : base(text, EntryType.LineComment) { }

        public override string ToString()
        {
            return string.Format("lc: {0}", text);
        }
    }

    public class SeparatorBlock : LocTextEntry
    {
        public SeparatorBlock() : base("=", EntryType.Separator) { }

        public override string ToString()
        {
            return "=";
        }
    }

    public class SemicolonBlock : LocTextEntry
    {
        public SemicolonBlock() : base(";", EntryType.Semicolon) { }

        public override string ToString()
        {
            return "\"; \"";
        }
    }

    public class LocPairBlock : LocTreeEntry
    {
        KeyBlock keyBlock = null;
        ValueBlock valueBlock = null;

        public string Key
        {
            get
            {
                return keyBlock?.text;
            }
        }
        public string Value
        {
            get
            {
                return valueBlock?.text;
            }
        }

        public LocPairBlock() : base(EntryType.LocPair) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("kv: ");
            foreach (var entry in entries)
            {
                sb.Append(entry.ToString());
                sb.Append("; ");
            }

            return sb.ToString();
        }

        internal override void FinalizeTree()
        {
            base.FinalizeTree();

            foreach (var entry in entries)
            {
                switch (entry.type)
                {
                    case EntryType.Key:
                        keyBlock = entry as KeyBlock;
                        break;
                    case EntryType.Value:
                        valueBlock = entry as ValueBlock;
                        break;
                }
            }
        }
    }

    public class KeyBlock : LocTextEntry
    {
        public KeyBlock(string text) : base(text, EntryType.Key) { }

        public override string ToString()
        {
            return string.Format("kb: {0}", text);
        }
    }

    public class ValueBlock : LocTextEntry
    {
        public ValueBlock(string text) : base(text, EntryType.Value) { }

        public override string ToString()
        {
            return string.Format("vb: {0}", text);
        }
    }

    public class LocFile : LocTreeEntry
    {
        public LocFile() : base(EntryType.Document) { }
        public IEnumerable<LocEntry> localizableEntries { get; private set; }

        internal override void FinalizeTree()
        {
            base.FinalizeTree();

            localizableEntries = entries.Where(entry => entry.type == EntryType.LocPair);
        }
    }
}
