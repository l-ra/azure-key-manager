using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using ShamirSecretShareSchema;


namespace ShamirSecretShareSchema.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var scheme = Scheme.of(5,3);
            var secretIn=new byte[32];
            for (var i=0; i<secretIn.Length; i++){
                secretIn[i]=(byte)i;
            }
            var shares = scheme.split(secretIn);
            // split done 
            var keys = shares.Keys.Select(x=>x).ToArray();
            var j = new Dictionary<int,byte[]>();
            j.Add(keys[0], shares[keys[0]]);
            j.Add(keys[1], shares[keys[1]]);
            j.Add(keys[2], shares[keys[2]]);
            
            var secret = scheme.join(j);
            Assert.Equal(secret, secretIn);

            j=new Dictionary<int,byte[]>();
            j.Add(keys[0], shares[keys[0]]);
            j.Add(keys[1], shares[keys[1]]);
            j.Add(keys[3], shares[keys[3]]);
            secret = scheme.join(j);

            j=new Dictionary<int,byte[]>();
            j.Add(keys[0], shares[keys[0]]);
            j.Add(keys[1], shares[keys[1]]);
            j.Add(keys[4], shares[keys[4]]);
            secret = scheme.join(j);

            j=new Dictionary<int,byte[]>();
            j.Add(keys[2], shares[keys[2]]);
            j.Add(keys[1], shares[keys[1]]);
            j.Add(keys[3], shares[keys[3]]);
            secret = scheme.join(j);

        }
    }
}
