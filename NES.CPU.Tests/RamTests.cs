using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NES.CPU.Tests
{
    public class RamTests
    {
        [Fact]
        public void MIrroringDoesMirroringAndSHit()
        {
            var ramDevice = new Ram(new AddressRange(0x0100, 0x0900), 0x0200);
            ramDevice.Write(0x0101, 1);
            ramDevice.Write(0x08ff, 0xff);
            Assert.Equal(1, ramDevice.Read(0x0101));
            Assert.Equal(1, ramDevice.Read(0x0301));
            Assert.Equal(1, ramDevice.Read(0x0501));
            Assert.Equal(1, ramDevice.Read(0x0701));
            Assert.Equal(0xff, ramDevice.Read(0x02ff));
            Assert.Equal(0xff, ramDevice.Read(0x04ff));
            Assert.Equal(0xff, ramDevice.Read(0x06ff));
            Assert.Equal(0xff, ramDevice.Read(0x08ff));
        }
    }
}
