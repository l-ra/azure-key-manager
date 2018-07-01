using System;
using Xunit;
using ProquintUtil;

namespace ProquintUtil.Tests
{
    public class ProquintUtilTests
    {

        byte[] data = {
                            0xc1, 0xe3, 0x46, 0x20, 0x14, 0xb8, 0x17, 0x21, 
                            0xc7, 0x88, 0x72, 0x72, 0x0d, 0xd3, 0xab, 0x8e, 
                            0x1b, 0x0f, 0x69, 0x56, 0x18, 0xa0, 0x43, 0x02,
                            0x8e, 0x0e, 0x50, 0x68, 0x01, 0xba, 0x66, 0x8e
                      };
        string hex = "c1e3462014b81721c78872720dd3ab8e1b0f695618a043028e0e506801ba668e";
        string quints = "salog-himob-difum-disod-sivam-lanuf-bulig-povav-dosaz-kojik-dofob-hasaf-mumav-jadom-bakup-kipav";

        [Fact]
        public void Test1()
        {
            Assert.Equal(
                PQ.bytes2quints(data),
                quints
            );

            Assert.Equal(
                PQ.quints2bytes(quints),
                data
            );

            Assert.Equal(
                PQ.parseHex(hex),
                data
            );
        }
    }
}
