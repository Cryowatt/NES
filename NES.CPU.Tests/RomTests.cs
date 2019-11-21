using System;
using System.Collections.Generic;
using System.IO;
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
            }
        }
    }
}
