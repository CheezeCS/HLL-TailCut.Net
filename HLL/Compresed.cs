using System;
using System.Collections.Generic;

namespace HLL
{
    public class CompresedList : ICompressedIterable
    {
        uint count;
        uint last;
        internal VariableLengthList b;
        internal CompresedList()
        {
            b = new VariableLengthList();
        }
        CompresedList(uint count, uint last, VariableLengthList b)
        {
            this.count = count;
            this.last = last;
            this.b = b;
        }

        internal object Clone()
        {
            return new CompresedList(count, last, (VariableLengthList)b.Clone());
        }
        internal void Append(uint x)
        {
            count++;
            b.Append(x - last);
        }
        internal uint Count()
        {
            return count;
        }
        public (uint, int) Decode(int i, uint last)
        {
            var ni =b.Decode(i, last);
            return (ni.Item1 + last, ni.Item2);
        }

        public Iterator GetIterator()
        {
            return new Iterator(0,0,this);
        }

        public int Length()
        {
            return b.Length;
        }

    }
    internal class VariableLengthList :  ICloneable
    {
        List<byte> b;
        internal VariableLengthList()
        {
            b = new List<byte>();
        }
        internal VariableLengthList(List<byte> b)
        {
            this.b = b;
        }
        public void Append(uint x)
        {
            while((x & 0xffffff80) != 0)
            {
                b.Add((byte)((x & 0x7f) | 0x80));
                x >>= 7;
            }
            b.Add((byte)(x & 0x7f));
        }
        public int Length
        {
            get => b.Count;
        }

        public object Clone()
        {
            return new VariableLengthList(b);
        }
        internal (uint,int) Decode(int i, uint last)
        {
            uint x = 0; 
            var j = i;
            for (; (b[j]&0x80) !=0;j++)
            {
                x |= (uint)(b[j]&0x7f) << ((j-i)*7);
            }
            x |= (uint)b[j] << ((j - i) * 7);
            return (x, j + 1);
        }
    }
    public class Iterator
    {
        int i;
        uint last;
        ICompressedIterable v;
        internal Iterator(int i, uint last, ICompressedIterable v)
        {
            this.i = i;
            this.last = last;
            this.v = v;
        }

        internal uint Next()
        {
            (last, i) = v.Decode(i, last);
            return last;
        }
        internal uint Peek()
        {
            (var n, _) = v.Decode(i, last);
            return n;
        }
        internal bool HasNext()
        {
            return i < v.Length();
        }
    }
}
