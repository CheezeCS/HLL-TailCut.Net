using System.Security.Cryptography;
using System.Text;

namespace HLLTest
{
    public class Tests
    {
        MD5 md5;
        [SetUp]
        public void Setup()
        {
            md5 = MD5.Create();
        }

        [Test]
        public void EstimationTest()
        {
            var hll = new HyperLogLog(14, false);
            var step = 10;

            for(uint i=1; i< 10000000; i++)
            {
                var str = md5.ComputeHash(Encoding.UTF8.GetBytes($"randstring-{i}"));
                hll.InsertHash(str);
                if(i%step==0)
                {
                    step *= 5;
                    var est = hll.Estimate();
                    var ratio = EstimateError(est, i);
                    if(ratio>2)
                    {
                        Assert.Fail($"Got {est}, exp {i}, delta {ratio * 100}%");
                    }
                }
            }
            Assert.Pass();
        }
        public double EstimateError(ulong got, ulong exp)
        {
            double delta;
            if(got> exp)
            {
                delta = got - exp;
            }
            else
            {
                delta = exp- got;
            }
            return delta/exp;
        }
        
    }
}