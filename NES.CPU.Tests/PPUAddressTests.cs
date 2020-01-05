using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NES.CPU.Tests
{
    public class PPUAddressTests
    {
        // yyy NN YYYYY XXXXX
        // ||| || ||||| +++++-- coarse X scroll
        // ||| || +++++-------- coarse Y scroll
        // ||| ++-------------- nametable select
        // +++----------------- fine Y scroll

        [Fact]
        public void CoarseXTest()
        {
            var a = new PPUAddress();
            a.CoarseX = 0b11111;
            Assert.Equal(0b11111, a.CoarseX);
            Assert.Equal(0b11111, a.Ptr);
            a.CoarseX++;
            Assert.Equal(0, a.CoarseX);
            Assert.Equal(0, a.Ptr);
        }

        [Fact]
        public void CoarseYTest()
        {
            var a = new PPUAddress();
            a.CoarseY = 0b11111;
            Assert.Equal(0b11111, a.CoarseY);
            Assert.Equal(0b1111100000, a.Ptr);
            a.CoarseY++;
            Assert.Equal(0, a.CoarseY);
            Assert.Equal(0, a.Ptr);
        }

        [Fact]
        public void NametableTest()
        {
            var a = new PPUAddress();
            a.Nametable = 0b11;
            Assert.Equal(0b11, a.Nametable);
            Assert.Equal(0b110000000000, a.Ptr);
            a.Nametable++;
            Assert.Equal(0, a.Nametable);
            Assert.Equal(0, a.Ptr);
        }

        [Fact]
        public void FineYTest()
        {
            var a = new PPUAddress();
            a.FineY = 0b111;
            Assert.Equal(0b111, a.FineY);
            Assert.Equal(0b111000000000000, a.Ptr);
            a.FineY++;
            Assert.Equal(0, a.FineY);
            Assert.Equal(0, a.Ptr);
        }
    }
}
