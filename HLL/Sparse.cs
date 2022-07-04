using System;
using System.Numerics;

namespace HLL
{
    internal static class Sparse
    {
        internal static uint GetIndex(uint k, byte p, byte pp)
        {
            if((k&1) ==1)
            {
                return Helpers.Bextr32(k, (byte)(32 - p), pp);
            }
            return Helpers.Bextr32(k, (byte)(pp - p + 1), p);
        }
        internal static uint EncodeHash(UInt64 x, byte p, byte pp)
        {
            var idx = (uint)Helpers.Bextr(x, (byte)(64 - pp), p);
            if(Helpers.Bextr(x, (byte)(64 - pp), (byte)(pp-p))==0)
            {
                var zeros = BitOperations.LeadingZeroCount((Helpers.Bextr(x,0,(byte)(64-pp))<<pp)|((UInt64)1 <<pp-1))+1;
                return idx << 7 | (uint)(zeros << 1) | 1;
            }
            return idx << 1;
        }
        internal static (uint,byte) DecodeHash(uint k, byte p, byte pp)
        {
            byte r;
            if((k&1) == 1)
            {
                r = (byte)((byte)Helpers.Bextr32(k, 1, 6) + pp - p);
            }
            else
            {
                r = (byte)(BitOperations.LeadingZeroCount(k << (32 - pp + p - 1)) - 31);
            }
            return (GetIndex(k, p, pp), r);
        }
    }
}
