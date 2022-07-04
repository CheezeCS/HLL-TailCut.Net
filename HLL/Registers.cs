using System;

namespace HLL
{
    internal class Registers : ICloneable
    {
        internal Reg[] tailcuts;
        uint nz;

        public Registers(uint size)
        {
            tailcuts = new Reg[size/2];
            for(int i = 0; i < tailcuts.Length; i++)
            {
                tailcuts[i] = new Reg();
            }
            nz = size;
        }
        Registers(Reg[] tailcuts, uint nz)
        {
            this.tailcuts = tailcuts;
            this.nz = nz;
        }

        public object Clone()
        {
            return new Registers((Reg[])tailcuts.Clone(),nz);
        }
        public void Rebase(byte delta)
        {
            nz = (uint)(tailcuts.Length * 2);
            for(uint i=0; i < tailcuts.Length; i++ )
            {
                for(byte j = 0; j <2; j++)
                {
                    var val = tailcuts[i].Get(j);
                    if (val>= delta)
                    {
                        tailcuts[i].Set(j, (byte)(val - delta));
                        if(val-delta > 0)
                        {
                            nz--;
                        }
                    }
                }
            }
        }
        public void Set(uint i, byte val)
        {
            var offset = (byte)(i & 1);
            var index = i / 2;
            if (tailcuts[index].Set(offset,val))
            {
                nz--;
            }
        }
        public byte Get(uint i)
        {
            var offset = (byte)(i & 1);
            var index = i / 2;
             return tailcuts[index].Get(offset);
        }
        public (double res, double ez) SumAndZeroes(byte _base)
        {
            double ez =0, res=0;
            for(int i = 0; i < tailcuts.Length; i++)
            {
                for(byte j = 0; j < 2; j++)
                {
                    double v = _base + tailcuts[i].Get(j);
                    if(v == 0)
                    {
                        ez++;
                    }
                    res += 1.0 / Math.Pow(2.0, v);
                }
            }
            nz = (uint)ez;
            return (res, ez);
        }
        public byte Min()
        {
            if (nz > 0)
            {
                return 0;
            }
            var min = byte.MaxValue;
            for(int i = 0; i < tailcuts.Length; i++)
            {
                if (tailcuts[i].r ==0 || min ==0)
                {
                    return 0;
                }
                var val = (byte)(tailcuts[i].r << 4 >> 4);
                if(val < min)
                {
                    min = val;
                }
                val = (byte)(tailcuts[i].r >> 4);
                if(val < min)
                {
                    min = val;
                }
            }
            return min;
        }
    }
    internal struct Reg
    {
        internal byte r;
        internal bool Set(byte offset, byte val)
        {
            bool isZero;
            if (offset == 0)
            {
                isZero = r < 16;
                var tmpVal = (byte)((byte)(r << 4) >> 4);
                r = (byte)(tmpVal | (val << 4));
            }
            else
            {
                isZero = (r & 0x0f) == 0;
                var tmpVal = (byte)((byte)(r >> 4) << 4);
                r = (byte)(tmpVal | val);
            }
            return isZero;
        }
        internal byte Get(byte offset)
        {
            if(offset == 0 )
            {
                return (byte)(r >> 4);
            }
            return (byte)((byte)(r << 4) >> 4);
        }
    }

}
