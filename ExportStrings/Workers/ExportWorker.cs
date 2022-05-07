using ExportStrings.Configuration;
using ExportStrings.LocProject;
using ExportStrings.Verbs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.Workers
{
    class ExportWorker : BaseWorker<Verbs.Export>
    {
        public ExportWorker(Settings settings, Verbs.Export verb) : base(settings, verb) { }

        public override void Do()
        {
            List<StringsFile> infos = new List<StringsFile>();

            foreach (var path in verb.Input)
            {
                var input = ResolvePaths(path, verb.Filter);
                foreach (var line in input)
                {
                    var info = new StringsFile(line);
                    infos.Add(info);
                    //Console.WriteLine(info);
                }
            }

            var projects = CreateProjects(infos).ToArray();

            foreach (var prj in projects)
            {
                CreateTable(prj, verb);
            }

        }



        static IEnumerable<IEnumerable<string>> CreateTable(LocalizationProject prj, Verbs.Export options)
        {
            SortedSet<string> keys = new SortedSet<string>();

            foreach (var file in prj.Files)
            {
                keys.UnionWith(file.SemanticTree.localizableEntries.Select(x => x.Key));
            }

            IEnumerable<IEnumerable<string>> GenTable()
            {
                IEnumerable<string> GenHeader()
                {
                    yield return "";
                    if (!options.KeysOnly)
                    {
                        foreach (var file in prj.Files)
                        {
                            yield return file.Locale;
                        }
                    }
                }


                IEnumerable<string> GenRow(string key)
                {
                    yield return key;
                    if (!options.KeysOnly)
                    {
                        foreach (var file in prj.Files)
                        {
                            if (file.SemanticTree.localizableEntries.Contains(key))
                            {
                                yield return file.SemanticTree.localizableEntries[key].Last().Value;
                            }
                            else
                            {
                                Console.WriteLine($"Key {key} is missing in file:\n{file.Path}");
                                yield return "";
                            }

                        }
                    }
                }

                if (options.Header)
                {
                    yield return GenHeader();
                }

                foreach (var key in keys)
                {
                    yield return GenRow(key);
                }

                yield break;
            }

            return GenTable();
            //using (TextWriter test = new StreamWriter(new FileStream($"D:\\test{testI++}.scv", FileMode.Create, FileAccess.Write, FileShare.Read)))
            //{
            //    CSV.Write(test, GenTable());
            //}
            //Console.ReadKey();
        }

        IEnumerable<LocalizationProject> CreateProjects(IEnumerable<StringsFile> files)
        {
            if (verb.SingleFile)
            {
                return new LocalizationProject(null, files).Yield();
            }
            else
            {
                return files
                    .GroupBy(x => x.ProjectPath)
                    .Select(x => new LocalizationProject(x.Key, x));
            }
        }

        IEnumerable<string> ResolvePaths(string path, string filter)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
            else
            {
                foreach (var file in Directory.GetFiles(path, filter, SearchOption.AllDirectories))
                {
                    yield return Path.GetFullPath(file);
                }
            }
        }


    }
}
