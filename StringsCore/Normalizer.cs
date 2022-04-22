using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsCore
{
    internal static class LinkedListInserts
    {
        public static LinkedListNode<T> InsertBefore<T>(this LinkedList<T> list, LinkedListNode<T> node, T item)
        {
            if (node == null)
            {
                return list.AddLast(item);
            }
            else
            {
                return list.AddBefore(node, item);
            }
        }

        public static LinkedListNode<T> InsertAfter<T>(this LinkedList<T> list, LinkedListNode<T> node, T item)
        {
            if (node == null)
            {
                return list.AddFirst(item);
            }
            else
            {
                return list.AddAfter(node, item);
            }
        }
    }

    public class Normalizer
    {


        public void Normalize(LocFile document)
        {
            var entryList = new LinkedList<LocEntry>(document.entries);
            /*List<LinkedListNode<LocEntry>> nodes = new List<LinkedListNode<LocEntry>>();
            var node = entryList.First;
            while (node != null)
            {
                nodes.Add(node);
                node = node.Next;
            }*/


            // Разбиваем текстовые блоки по переносам строк
            var node = entryList.First;

            while (node != null)
            {
                var current = node;
                node = node.Next;
                if (current.Value.Type == LocEntry.EntryType.Text)
                {
                    var text = (current.Value as TextBlock).Text;
                    var parts = text.Split('\n');

                    var prew = current.Previous;
                    entryList.Remove(current);

                    bool isFirst = true;
                    foreach (var part in parts)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            prew = entryList.InsertAfter(prew, new NewLineBlock());
                        }

                        if (part.Length > 0)
                        {
                            prew = entryList.InsertAfter(prew, new TextBlock(part));
                        }
                    }
                }

            }

            // Переносим начала и концы строк внутрь соответствующих LocPairBlock'ов, если необходимо
            node = entryList.First;

            while (node != null)
            {
                if (node.Value.Type == LocEntry.EntryType.LocPair)
                {
                    var prevNode = node.Previous;
                    while (prevNode != null && 
                           !(prevNode.Value is NewLineBlock) &&
                           !(prevNode.Value is LineCommentBlock) &&
                           !(prevNode.Value is LocPairBlock))
                    {
                        (node.Value as LocPairBlock).entries.Insert(0, prevNode.Value);
                        var tmpNode = prevNode;
                        prevNode = prevNode.Previous;
                        entryList.Remove(tmpNode);
                    }


                    while (node.Next != null)
                    {
                        if (node.Next.Value is LocPairBlock)
                        {
                            break;
                        }
                        else if (node.Next.Value is LineCommentBlock ||
                                   node.Next.Value is NewLineBlock)
                        {
                            (node.Value as LocPairBlock).Append(node.Next.Value);
                            entryList.Remove(node.Next);
                            break;
                        }
                        else
                        {
                            (node.Value as LocPairBlock).Append(node.Next.Value);
                            entryList.Remove(node.Next);
                        }
                    }

                }

                node = node.Next;
            }

            document.entries.Clear();
            document.entries.AddRange(entryList);

        }
    }
}
