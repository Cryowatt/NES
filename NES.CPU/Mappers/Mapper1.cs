using System;
using System.Buffers;

namespace NES.CPU.Mappers
{
    public class Mapper1 : IMapper
    {
        private Memory<byte> prgRam = new Memory<byte>(new byte[0x2000]);
        private Memory<byte> chrRam = new Memory<byte>(new byte[0x4000]);
        private readonly RomImage image;
        private MemoryHandle characterRomBank0;
        private MemoryHandle characterRomBank1;
        private MemoryHandle programRomBank0;
        private MemoryHandle programRomBank1;
        private byte prgShift = 0b10000;
        private byte controlRegister;

        public Mapper1(RomImage image)
        {
            this.image = image;

            if (this.image.CharacterRomData.Length > 0)
            {
                this.characterRomBank0 = image.CharacterRomData.Slice(0, 0x1000).Pin();
                this.characterRomBank1 = image.CharacterRomData.Slice(0x1000, 0x1000).Pin();
            }
            else
            {
                this.characterRomBank0 = chrRam.Pin();
            }

            this.programRomBank0 = image.ProgramRomData.Slice(0, 0x4000).Pin();
            this.programRomBank1 = image.ProgramRomData.Slice(image.ProgramRomData.Length - 0x4000, 0x4000).Pin();
        }

        public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

        public ReadOnlyMemory<byte> PatternTable
        {
            get
            {
                if (this.image.CharacterRomData.Length > 0)
                {
                    return this.image.CharacterRomData.Slice(0, 0x2000);
                }
                else
                {
                    return this.chrRam.Slice(0, 0x2000);
                }
            }
        }

        unsafe byte IBusDevice.Read(Address address)
        {
            if (0x6000 <= address.Ptr && address.Ptr < 0x8000)
            {
                //CPU $6000-$7FFF: 8 KB PRG RAM bank, (optional)
                return prgRam.Span[address - 0x06000];
            }
            else if (0x8000 <= address.Ptr && address.Ptr < 0xC000)
            {
                //CPU $8000-$BFFF: 16 KB PRG ROM bank, either switchable or fixed to the first bank
                return *((byte*)programRomBank0.Pointer + (address.Ptr - 0x8000));
            }
            else if (0xC000 <= address.Ptr)
            {
                //CPU $C000-$FFFF: 16 KB PRG ROM bank, either fixed to the last bank or switchable
                return *((byte*)programRomBank1.Pointer + (address.Ptr - 0xC000));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IBusDevice.Write(Address address, byte value)
        {
            //CPU $8000-$FFFF
            if (0x8000 <= address.Ptr)
            {
                if ((value & 0x8000) > 0)
                {
                    prgShift = 0;
                }
                else
                {
                    if ((prgShift & 0x1) > 0)
                    {
                        byte regValue = (byte)((prgShift >> 1) | ((value & 0x1) << 5));
                        switch ((address.High & 0x60) >> 5)
                        {
                            case 0: //control
                                throw new NotImplementedException();
                                controlRegister = regValue;
                                break;
                            case 1: //chr0
                                throw new NotImplementedException();
                                characterRomBank0 = image.CharacterRomData.Slice(regValue * 0x4000, 0x4000).Pin();
                                break;
                            case 2: //chr1
                                throw new NotImplementedException();
                                characterRomBank1 = image.CharacterRomData.Slice(regValue * 0x4000, 0x4000).Pin();
                                break;
                            case 3: //prg
                                throw new NotImplementedException();
                                //4bit0
                                //-----
                                //CPPMM
                                //|||||
                                //|||++- Mirroring (0: one-screen, lower bank; 1: one-screen, upper bank;
                                //|||               2: vertical; 3: horizontal)
                                //|++--- PRG ROM bank mode (0, 1: switch 32 KB at $8000, ignoring low bit of bank number;
                                //|                         2: fix first bank at $8000 and switch 16 KB bank at $C000;
                                //|                         3: fix last bank at $C000 and switch 16 KB bank at $8000)
                                //+----- CHR ROM bank mode (0: switch 8 KB at a time; 1: switch two separate 4 KB banks)
                                //this.controlRegister
                                characterRomBank0 = image.CharacterRomData.Slice(regValue * 0x4000, 0x4000).Pin();
                                break;
                        }

                        prgShift = 0b10000;
                    }
                    else
                    {
                        prgShift >>= 1;
                        prgShift |= (byte)((value & 0x1) << 5);
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        unsafe byte IPPUBusDevice.Read(Address address)
        {
            //PPU $0000-$0FFF: 4 KB switchable CHR bank
            //PPU $1000-$1FFF: 4 KB switchable CHR bank

            if (address.Ptr < 0x1000)
            {
                return *((byte*)characterRomBank0.Pointer + address.Ptr);
            }
            else if (0x1000 <= address.Ptr && address.Ptr < 0x2000)
            {
                return *((byte*)characterRomBank1.Pointer + (address.Ptr - 0x1000));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        void IPPUBusDevice.Write(Address address, byte value)
        {
            throw new NotImplementedException();
        }
    }
}

/*
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
 */
