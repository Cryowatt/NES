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
        public void Basics()
        {
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/01-basics.nes")))
            {
                var romFile = RomImage.From(reader);
                var cpu = new Ricoh2A(new NesBus(new Mapper0(romFile)));
                foreach (var cycle in cpu.Process().Take(10000000))
                {
                }
            }
        }
    }
}
