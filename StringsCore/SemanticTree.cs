using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsCore
{
    public abstract class LocEntry : ICloneable
    {
        public enum EntryType
        {
            Text,
            BlockComment,
            LineComment,
            Separator,
            Semicolon,
            NewLine,
            Key,
            Value,
            LocPair,
            Document
        }

        public EntryType Type { get; private set; }

        public LocEntry() { }

        public LocEntry(EntryType type)
        {
            this.Type = type;
        }

        public object Clone()
        {
            var copy = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(this.GetType()) as LocEntry;
            CopyTo(copy);
            return copy;
        }

        protected virtual void CopyTo(LocEntry o)
        {
            o.Type = this.Type;
        }
    }

    public abstract class LocTextEntry : LocEntry
    {
        public string Text;

        public LocTextEntry(string text, EntryType type) : base(type)
        {
            this.Text = text;
        }

        protected override void CopyTo(LocEntry o)
        {
            base.CopyTo(o);
            (o as LocTextEntry).Text = this.Text;
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

        public virtual void FinalizeTree()
        {
            foreach (LocEntry entry in entries)
            {
                if (entry is LocTreeEntry)
                {
                    (entry as LocTreeEntry).FinalizeTree();
                }
            }
        }

        protected override void CopyTo(LocEntry o)
        {
            base.CopyTo(o);
            var copy = o as LocTreeEntry;
            copy.entries = new List<LocEntry>();

            foreach (var entry in entries)
            {
                copy.Append(entry.Clone() as LocEntry);
            }
        }
    }

    public class TextBlock : LocTextEntry
    {
        public TextBlock(string text) : base(text, EntryType.Text) { }

        public override string ToString()
        {
            return String.Format("t: {0}", Text);
        }
    }

    public class BlockCommentBlock : LocTextEntry
    {
        public BlockCommentBlock(string text) : base(text, EntryType.BlockComment) { }

        public override string ToString()
        {
            return String.Format("bc: {0}", Text);
        }
    }

    public class LineCommentBlock : LocTextEntry
    {
        public LineCommentBlock(string text) : base(text, EntryType.LineComment) { }

        public override string ToString()
        {
            return string.Format("lc: {0}", Text);
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

    public class NewLineBlock : LocTextEntry
    {
        public NewLineBlock() : base("\n", EntryType.NewLine) { }

        public override string ToString()
        {
            return "nl";
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
                return keyBlock?.Text;
            }
            set
            {
                keyBlock.Text = value;
            }
        }
        public string Value
        {
            get
            {
                return valueBlock?.Text;
            }
            set
            {
                valueBlock.Text = value;
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

        public override void FinalizeTree()
        {
            base.FinalizeTree();

            FindKeyValueEntries();
        }

        protected override void CopyTo(LocEntry o)
        {
            base.CopyTo(o);
            var copy = o as LocPairBlock;

            copy.FindKeyValueEntries();
        }

        private void FindKeyValueEntries()
        {
            foreach (var entry in entries)
            {
                switch (entry.Type)
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
        public KeyBlock(string text, bool inline = false) : base(text, EntryType.Key) 
        {
            Inline = inline; 
        }

        public bool Inline = false;

        public override string ToString()
        {
            return string.Format("kb: {0}", Text);
        }
    }

    public class ValueBlock : LocTextEntry
    {
        public ValueBlock(string text) : base(text, EntryType.Value) { }

        public override string ToString()
        {
            return string.Format("vb: {0}", Text);
        }
    }

    public class LocFile : LocTreeEntry
    {
        public LocFile() : base(EntryType.Document) { }
        public ILookup<string, LocPairBlock> localizableEntries { get; protected set; }

        override public void FinalizeTree()
        {
            base.FinalizeTree();

            CacheLocEntries();
        }

        private void CacheLocEntries()
        {
            localizableEntries = entries.Where(x => x.Type == EntryType.LocPair).Cast<LocPairBlock>().ToLookup(x => x.Key);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("doc: ");
            foreach (var entry in entries)
            {
                sb.Append(entry.ToString());
                sb.Append("; ");
            }

            return sb.ToString();
        }

        protected override void CopyTo(LocEntry o)
        {
            base.CopyTo(o);
            var copy = o as LocFile;

            copy.CacheLocEntries();
        }
    }
}
