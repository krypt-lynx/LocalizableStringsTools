using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringsCore
{
    public class Range
    {
        public int Location;
        public int Lenght;
    }

    public class Block
    {
        public string Text;
        public string LocalizationKey;
        public Range LocalizationKeyRange;
        public string LocalizationValue;
        public Range LocalizationValueRange;
    }
}
