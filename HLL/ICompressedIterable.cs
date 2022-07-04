using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLL
{
    internal interface ICompressedIterable
    {
        public (uint, int) Decode(int i, uint last);
        public int Length();
        public Iterator GetIterator();
    }
}
