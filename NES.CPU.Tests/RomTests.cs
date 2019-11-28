using NES.CPU.Mappers;
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
                var bus = new NesBus(new Mapper0(romFile));
                var cpu = new Ricoh2A(bus);
                int howFuckingFarDidIEvenGet = 0;
                var process = cpu.Process();
                var bootStrap = process.TakeWhile(o => bus.Read(0x6000) != 0x80).Count();

                foreach (var cycle in process)
                {
                    howFuckingFarDidIEvenGet++;
                    Assert.Equal(0x80, bus.Read(0x6000));
                }
            }
        }
    }
}
