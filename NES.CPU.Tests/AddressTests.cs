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

        [Fact]
        public void EqualsOperator()
        {
            Address ptr = 0xaa55;
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(ptr == ptr);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.True(ptr == 0xaa55);
            Assert.True(ptr != 0x55aa);
            Assert.True(ptr.Equals(0xaa55));
            Assert.True(ptr.Equals((object)(Address)0xaa55));
            Assert.Equal(ptr, (Address)0xaa55);
            Assert.StrictEqual(ptr, (Address)0xaa55);
            Assert.NotEqual(ptr, (Address)0x55aa);
            Assert.NotStrictEqual(ptr, (Address)0x55aa);
        }

        [Fact]
        public void ToStringFormatting()
        {
            Assert.Equal("0xFFFF", ((Address)0xffff).ToString());
        }
    }
}
