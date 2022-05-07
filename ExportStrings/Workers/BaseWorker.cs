using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportStrings.Workers
{
    abstract class BaseWorker<TVerb>
    {
        protected Configuration.Settings settings;
        protected TVerb verb;

        public BaseWorker(Configuration.Settings settings, TVerb verb)
        {
            this.settings = settings;
            this.verb = verb;
        }

        public abstract void Do();
    }
}
