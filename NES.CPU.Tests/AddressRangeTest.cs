using Xunit;

namespace NES.CPU.Tests
{
    public class AddressRangeTest
    {
        [InlineData(0x0000, 0x0000, 0x0000)]
        [InlineData(0x0000, 0xffff, 0x8000)]
        [InlineData(0xffff, 0xffff, 0xffff)]
        [InlineData(0x1000, 0x2000, 0x1000)]
        [InlineData(0x1000, 0x2000, 0x2000)]
        [Theory]
        public void MaskContainsAddress(ushort start, ushort end, ushort testAddress)
        {
            Assert.True(new AddressRange(start, end).Contains(testAddress));
        }

        [InlineData(0x0000, 0x0000, 0x0001)]
        [InlineData(0xffff, 0xffff, 0xfffe)]
        [InlineData(0x1000, 0x2000, 0x0fff)]
        [InlineData(0x1000, 0x2000, 0x2001)]
        [Theory]
        public void MaskDoesNotContainsAddress(ushort start, ushort end, ushort testAddress)
        {
            Assert.False(new AddressRange(start, end).Contains(testAddress));
        }
    }
}
