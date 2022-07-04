using System;
using System.Collections.Generic;

namespace HLL
{
    public class HyperLogLog : ICloneable
    {
        const byte pp = 25;
        const byte capacity = 16;
        const uint mp = (uint)1 << pp;

        byte p;
        byte b;
        uint m;
        double alpha;
        HashSet<uint> tmpSet;
        CompresedList sparseList;
        Registers regs;
        HyperLogLog() { }
        public HyperLogLog(byte precision, bool sparse)
        {
            if(precision < 4 || precision > 18)
            {
                throw new ArgumentException("Precision must be in range from 4 to 18.");
            }
            m = (uint)Math.Pow(2, precision);
            p = precision;
            alpha = m switch
            {
                16 => 0.673,
                32 => 0.697,
                64 => 0.709,
                _ => 0.7213 / (1 + 1.079 / m),
            };
            if (sparse)
            {
                tmpSet = new HashSet<uint>();
                sparseList = new CompresedList();
            }
            else
            {
                regs = new Registers(m);
            }
        }
        public object Clone()
        {
            return new HyperLogLog
            {
                b = b,
                p = p,
                m = m,
                alpha = alpha,
                tmpSet = tmpSet == null ? null : new HashSet<uint>(),
                sparseList = sparseList == null ? null : (CompresedList)sparseList.Clone(),
                regs = regs == null ? null : (Registers)regs.Clone()
            };
        }
        public UInt64 Estimate()
        {
            if(IsSparse)
            {
                MergeSparse();
                return (UInt64)Helpers.LinearCount(mp, mp - sparseList.Count());
            }
            (var sum, var ez) = regs.SumAndZeroes(b);
            double fm = m;
            double est;
            Func<double, double> beta;
            if(p<16)
            {
                beta = Helpers.Beta14;
            }
            else
            {
                beta = Helpers.Beta16;
            }
            if(b==0)
            {
                est = alpha * fm * (fm - ez) / (sum + beta(ez));
            }
            else
            {
                est = alpha * fm * fm / sum;
            }
            return (UInt64)(est + 0.5);
        }
        public bool InsertHash(byte[] hash)
        {
            var x = BitConverter.ToUInt64(hash, 0) ^ BitConverter.ToUInt64(hash, 8);
            if (IsSparse)
            {
                var changed = tmpSet.Add(Sparse.EncodeHash(x, p, pp));
                if(!changed) return false;
                if(tmpSet.Count *100 > m/2)
                {
                    MergeSparse();
                    if(sparseList.Length() > m/2)
                    {
                        ToNormal();
                    }
                }
                return true;
            }
            else
            {
                (var i, var r) = Helpers.GetPosVal(x, p);
                return InsertIntoRegisters((uint)i, r);
            }
        }
        bool IsSparse
        {
            get => sparseList != null;
        }
        void MaybeToNormal()
        {
            if(tmpSet.Count*100 > m)
            {
                MergeSparse();
                if (sparseList.Length() > m)
                {
                    ToNormal();
                }
            }
        }
        void ToNormal()
        {
            if(tmpSet.Count > 0)
            {
                MergeSparse();
            }
            regs = new Registers(m);
            for (var iter = sparseList.GetIterator(); iter.HasNext();)
            {
                (var i, var r) = Sparse.DecodeHash(iter.Next(), p, pp);
                InsertIntoRegisters(i, r);
            }
            tmpSet = null;
            sparseList = null;
        }
        bool InsertIntoRegisters(uint i, byte r)
        {
            var changed = false;
            if(r - b >= capacity)
            {
                var db = regs.Min();
                if(db > 0)
                {
                    b += db;
                    regs.Rebase(db);
                    changed = true;
                }
            }
            if(r > b)
            {
                byte val = (byte)(r - b);
                byte c1 = capacity - 1;
                if (c1 < val)
                {
                    val = c1;
                }
                if(val > regs.Get(i))
                {
                    regs.Set(i, val);
                    changed = true;
                }
            }
            return changed;
        }
        void MergeSparse()
        {
            if(tmpSet.Count == 0)
            {
                return;
            }
            var keys = new uint[tmpSet.Count];
            tmpSet.CopyTo(keys, 0);
            Array.Sort(keys);
            var newList = new CompresedList();
            var iter = sparseList.GetIterator();
            for (var i = 0; i < keys.Length || iter.HasNext();)
            {
                if(!iter.HasNext())
                {
                    newList.Append(keys[i]);
                    i++;
                    continue;
                }
                if(i >= keys.Length)
                {
                    newList.Append(iter.Next());
                    continue;
                }
                var x1 = iter.Peek();
                var x2 = keys[i];
                if(x1 == x2)
                {
                    newList.Append(iter.Next());
                    i++;
                }
                else if (x1>x2)
                {
                    newList.Append(x2);
                    i++;
                }
                else
                {
                    newList.Append(iter.Next());
                }
            }
            sparseList = newList;
            tmpSet = new HashSet<uint>();
        }
        public void Merge(HyperLogLog other)
        {
            if (other == null)
            {
                return;
            }
            var cpOther = (HyperLogLog)other.Clone();
            if (p!=cpOther.p)
            {
                throw new InvalidOperationException($"Precisions of HLL vectors must be equal. Expected {p}, got {cpOther.p}");
            }
            if (IsSparse && cpOther.IsSparse)
            {
                foreach(var k in cpOther.tmpSet)
                {
                    tmpSet.Add(k);
                }
                for(var iter = cpOther.sparseList.GetIterator();iter.HasNext();)
                {
                    tmpSet.Add(iter.Next());
                }
                MaybeToNormal();
            }
            if(IsSparse)
            {
                ToNormal();
            }
            if(cpOther.IsSparse)
            {
                foreach(var k in cpOther.tmpSet)
                {
                    (var i, var r) = Sparse.DecodeHash(k, cpOther.p, pp);
                    InsertIntoRegisters(i, r);
                }
                for(var iter = cpOther.sparseList.GetIterator();iter.HasNext();)
                {
                    (var i, var r) = Sparse.DecodeHash(iter.Next(), cpOther.p, pp);
                    InsertIntoRegisters(i, r);
                }
            }
            else
            {
                if(b < cpOther.b)
                {
                    regs.Rebase((byte)(cpOther.b - b));
                    b = cpOther.b;
                }
                else
                {
                    cpOther.regs.Rebase((byte)(b - cpOther.b));
                    cpOther.b = b;
                }
                for(int i=0; i<cpOther.regs.tailcuts.Length;i++)
                {
                    var v1 = cpOther.regs.tailcuts[i].Get((byte)0);
                    if(v1> regs.Get((uint)i*2))
                    {
                        regs.Set((uint)i * 2,v1);
                    }
                    var v2 = cpOther.regs.tailcuts[i].Get(1);
                    if (v2 > regs.Get(1 + (uint)i * 2))
                    {
                        regs.Set(1+(uint)i * 2, v2);
                    }
                }
            }
            return;
        }
    }
}