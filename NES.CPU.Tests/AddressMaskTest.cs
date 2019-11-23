using Xunit;

namespace NES.CPU.Tests
{
    public class AddressMaskTest
    {
        [InlineData(0x0000, 0x0000, 0x0000)]
        [InlineData(0x0000, 0x0000, 0xffff)]
        [InlineData(0x0000, 0xffff, 0x0000)]
        [InlineData(0x1230, 0xfff0, 0x1230)]
        [InlineData(0x1230, 0xfff0, 0x123F)]
        [Theory]
        public void MaskContainsAddress(ushort start, ushort end, ushort testAddress)
        {
            Assert.True(new AddressRange(start, end).Contains(testAddress));
        }

        [InlineData(0x0000, 0xffff, 0xffff)]
        [InlineData(0x1230, 0xfff0, 0x122f)]
        [InlineData(0x1230, 0xfff0, 0x1240)]
        [Theory]
        public void MaskDoesNotContainsAddress(ushort start, ushort end, ushort testAddress)
        {
            Assert.False(new AddressRange(start, end).Contains(testAddress));
        }
    }
}
