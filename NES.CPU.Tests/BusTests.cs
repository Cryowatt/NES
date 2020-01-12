using NES.CPU.Mappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace NES.CPU.Tests
{
    public class NesBusTests
    {
        [Fact]
        public void Foo()
        {
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/01-basics.nes")))
            {
                var romFile = RomImage.From(reader);
                var mapper = Mapper.FromImage(romFile);
                var bus = new NesBus(mapper);
                bus.Read(0x2002);
            }
        }
    }
}
