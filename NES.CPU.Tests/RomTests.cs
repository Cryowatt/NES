using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace NES.CPU.Tests
{
    public class RomTests
    {
        [Fact]
        public void Test()
        {
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/01-basics.nes")))
            {
                var romFile = RomImage.From(reader);
                var cpu = new Ricoh2A(new NesBus(new Mapper0(romFile)));
                cpu.Process().Take(4).ToList();
            }
        }
    }
}
