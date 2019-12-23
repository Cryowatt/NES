using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace NES.CPU.Mappers
{
    public class Mapper0 : IMapper
    {
        private Memory<byte> ram = new Memory<byte>(new byte[0x1000]);
        private readonly RomImage image;
        private readonly MemoryHandle characterRomData;
        private readonly MemoryHandle programRomData;

        public Mapper0(RomImage image)
        {
            this.image = image;
            this.characterRomData = image.CharacterRomData.Pin();
            this.programRomData = image.ProgramRomData.Pin();
        }

        public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

        unsafe byte IBusDevice.Read(Address address)
        {
            //CPU $6000-$7FFF: Family Basic only: PRG RAM, mirrored as necessary to fill entire 8 KiB window, write protectable with an external switch
            if (0x6000 <= address.Ptr && address.Ptr < 0x8000)
            {
                return ram.Span[address - 0x06000];
            }
            else if (0x8000 <= address.Ptr && address.Ptr < 0xC000)
            {
                //CPU $8000-$BFFF: First 16 KB of ROM.
                return *((byte*)programRomData.Pointer + (address.Ptr - 0x8000));
            }
            else if (0xC000 <= address.Ptr && image.ProgramRomSize == 1)
            {
                //CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
                return *((byte*)programRomData.Pointer + (address.Ptr - 0xC000));
            }
            else if (0xC000 <= address.Ptr && image.ProgramRomSize == 2)
            {
                //CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
                return *((byte*)programRomData.Pointer + (address.Ptr - 0x8000));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IBusDevice.Write(Address address, byte value)
        {
            if (0x6000 <= address && address < 0x8000)
            {
                ram.Span[address - 0x06000] = value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        unsafe byte IPPUBusDevice.Read(Address address)
        {
            if (address.Ptr < 0x2000)
            {
                return *((byte*)this.characterRomData.Pointer + address.Ptr);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        unsafe void IPPUBusDevice.Write(Address address, byte value)
        {
            if (address.Ptr < 0x2000)
            {
                *((byte*)this.characterRomData.Pointer + address.Ptr) = value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}