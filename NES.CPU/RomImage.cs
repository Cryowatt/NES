using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NES.CPU
{
    public class RomImage
    {
        public byte ProgramSize { get; private set; }
        public byte CharacterSize { get; private set; }

        public static RomImage From(BinaryReader reader)
        {
            var header = new string(reader.ReadChars(4));
            
            if (header != "NES\x1a")
            {
                throw new BadImageFormatException();
            }
            
            var image = new RomImage();

            image.ProgramSize = reader.ReadByte();
            image.CharacterSize = reader.ReadByte();

            return image;
        }
    }
}
