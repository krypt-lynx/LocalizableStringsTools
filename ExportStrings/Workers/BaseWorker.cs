using ExportStrings.Configuration;
using ExportStrings.LocProject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.Workers
{
    abstract class BaseWorker<TVerb>
    {
        protected Configuration.Configuration config;
        protected TVerb verb;

        public BaseWorker(Configuration.Configuration config, TVerb verb)
        {
            this.config = config;
            this.verb = verb;
        }

        protected List<LocalizationProject> LoadProjects(IEnumerable<string> pathsOrNames)
        {
            List<LocalizationProject> projects = new List<LocalizationProject>();

            foreach (var input in pathsOrNames)
            {
                var paths = ResolvePath(input, out string name);
                if (name == null)
                {
                    var components = input.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    name = components.Last();
                }

                foreach (var path in paths)
                {
                    projects.Add(new LocalizationProject(path, name));
                }
            }

            return projects;
        }


        IEnumerable<string> ResolvePath(string input, out string match)
        {
            if (config.lprojects.TryGetValue(input, out LocalizableGroup group))
            {
                match = group.Name;
                return group.Paths;
            }
            else
            {
                match = null;
                return input.Yield();
            }

        }

        public abstract void Do();
    }
}
