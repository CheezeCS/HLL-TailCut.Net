using System;
using System.Numerics;

namespace HLL
{
    internal static class Helpers
    {
        internal static (UInt64, byte) GetPosVal(UInt64 x, byte p)
        {
            var i = Bextr(x, (byte)(64 - p), p);
            var w = x << p | (UInt64)1 << (p - 1);
            var rho = (byte)((byte)(BitOperations.LeadingZeroCount(w)) + 1);
            return (i, rho);
        }

        internal static uint Bextr32(uint v, byte start, byte length)
        {
            return (v >> start) & (((uint)1 << length) - 1);
        }
        internal static UInt64 Bextr(UInt64 v, byte start, byte length)
        {
            return (v >> start) & (((UInt64)1 << length) - 1);
        }
        internal static double LinearCount(uint m, uint v)
        {
            var fm = (double)m;
            return fm * Math.Log(fm / (double)v);
        }
        internal static double Beta14(double ez)
        {
            var zl = Math.Log(ez + 1);
            return -0.370393911 * ez +
                    0.070471823 * zl +
                    0.17393686 * Math.Pow(zl, 2) +
                    0.16339839 * Math.Pow(zl, 3) +
                    -0.09237745 * Math.Pow(zl, 4) +
                    0.03738027 * Math.Pow(zl, 5) +
                    -0.005384159 * Math.Pow(zl, 6) +
                    0.00042419 * Math.Pow(zl, 7);
        }
        internal static double Beta16(double ez)
        {
            var zl = Math.Log(ez + 1);
            return -0.37331876643753059 * ez +
                   -1.41704077448122989 * zl +
                   0.40729184796612533 * Math.Pow(zl, 2) +
                   1.56152033906584164 * Math.Pow(zl, 3) +
                   -0.99242233534286128 * Math.Pow(zl, 4) +
                   0.26064681399483092 * Math.Pow(zl, 5) +
                   -0.03053811369682807 * Math.Pow(zl, 6) +
                   0.00155770210179105 * Math.Pow(zl, 7);
        }
    }
}
