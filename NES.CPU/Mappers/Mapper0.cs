using System;
using System.IO.MemoryMappedFiles;

namespace NES.CPU.Mappers
{
    public class Mapper0 : IMapper
    {
        private Memory<byte> ram = new Memory<byte>(new byte[0x1000]);
        private readonly RomImage image;

        public Mapper0(RomImage image)
        {
            this.image = image;
        }

        public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

        public byte Read(Address address)
        {
            //CPU $6000-$7FFF: Family Basic only: PRG RAM, mirrored as necessary to fill entire 8 KiB window, write protectable with an external switch
            if (0x6000 <= address && address < 0x8000)
            {
                return ram.Span[address - 0x06000];
            }
            else if (0x8000 <= address && address < 0xC000)
            {
                //CPU $8000-$BFFF: First 16 KB of ROM.
                return image.ProgramRomData.Span[address - 0x8000];
            }
            else if (0xC000 <= address && image.ProgramRomData.Length == 0x4000)
            {
                //CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
                return image.ProgramRomData.Span[address - 0xC000];
            }
            else if (0xC000 <= address && image.ProgramRomData.Length == 0x8000)
            {
                //CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
                return image.ProgramRomData.Span[address - 0x8000];
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Write(Address address, byte value)
        {
            if (0x6000 <= address && address < 0x8000)
            {
                ram.Span[address - 0x06000] = value;
            }
        }
    }
}