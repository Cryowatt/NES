using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NES.CPU.Tests
{
    public class AddressTests
    {
        [Fact]
        public void HighLowCorrectness()
        {
            Address ptr = 0xaa55;
            Assert.Equal(0xaa55, ptr.Ptr);
            Assert.Equal(0xaa, ptr.High);
            Assert.Equal(0x55, ptr.Low);
        }

        [Fact]
        public void CanIncrementPointer()
        {
            Address ptr = 0x00ff;
            var prefix = ++ptr;
            Assert.Equal((Address)0x0100, ptr);
            Assert.Equal((Address)0x0100, prefix);
            var postfix = ptr++;
            Assert.Equal((Address)0x0100, postfix);
            Assert.Equal((Address)0x0101, ptr);
        }
    }
}
