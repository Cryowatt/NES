using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NES.CPU
{
    [Flags]
    public enum PPUControl : byte
    {
        Nametable0 = 0b0000_0000,
        Nametable1 = 0b0000_0001,
        Nametable2 = 0b0000_0010,
        Nametable3 = 0b0000_0011,
        NametableMask = 0b0000_0011,
        LineIncrement = 0b0000_0100,
        SpriteBank2 = 0b0000_1000,
        BGBank2 = 0b0001_0000,
        WideSprites = 0b0010_0000,
        ExtEnable = 0b0100_0000,
        NMIOnVBlank = 0b1000_0000,

        //7  bit  0
        //---- ----
        //VPHB SINN
        //|||| ||||
        //|||| ||++- Base nametable address
        //|||| ||    (0 = $2000; 1 = $2400; 2 = $2800; 3 = $2C00)
        //|||| |+--- VRAM address increment per CPU read/write of PPUDATA
        //|||| |     (0: add 1, going across; 1: add 32, going down)
        //|||| +---- Sprite pattern table address for 8x8 sprites
        //||||       (0: $0000; 1: $1000; ignored in 8x16 mode)
        //|||+------ Background pattern table address (0: $0000; 1: $1000)
        //||+------- Sprite size (0: 8x8 pixels; 1: 8x16 pixels)
        //|+-------- PPU master/slave select
        //|          (0: read backdrop from EXT pins; 1: output color on EXT pins)
        //+--------- Generate an NMI at the start of the
        //           vertical blanking interval (0: off; 1: on)
    }

    [Flags]
    public enum PPUStatus : byte
    {
        VBlank = 0b1000_0000,
        Spirte0Hit = 0b0100_0000,
        SpriteOverflow = 0b0010_0000,
        //This register reflects the state of various functions inside the PPU. It is often used for determining timing. To determine when the PPU has reached a given pixel of the screen, put an opaque (non-transparent) pixel of sprite 0 there.

        //7  bit  0
        //---- ----
        //VSO. ....
        //|||| ||||
        //|||+-++++- Least significant bits previously written into a PPU register
        //|||        (due to register not being updated for this address)
        //||+------- Sprite overflow. The intent was for this flag to be set
        //||         whenever more than eight sprites appear on a scanline, but a
        //||         hardware bug causes the actual behavior to be more complicated
        //||         and generate false positives as well as false negatives; see
        //||         PPU sprite evaluation. This flag is set during sprite
        //||         evaluation and cleared at dot 1 (the second dot) of the
        //||         pre-render line.
        //|+-------- Sprite 0 Hit.  Set when a nonzero pixel of sprite 0 overlaps
        //|          a nonzero background pixel; cleared at dot 1 of the pre-render
        //|          line.  Used for raster timing.
        //+--------- Vertical blank has started (0: not in vblank; 1: in vblank).
        //           Set at dot 1 of line 241 (the line *after* the post-render
        //           line); cleared after reading $2002 and at dot 1 of the
        //           pre-render line.
    }

    public unsafe class PPU : IBusDevice
    {
        private int cycle = 0;
        private int scanline = 0;
        private PPURegisters registers;
        private Address address;
        private PPUBus ppuBus;
        private MemoryHandle frameBufferHandle;
        private uint* pFrameBuffer;

        public Memory<int> FrameBuffer { get; }

        public PPU(IMapper mapper)
        {
            this.ppuBus = new PPUBus(mapper);
            FrameBuffer = new Memory<int>(new int[256 * 240]);
            //PPUCTRL ($2000)	0000 0000	0000 0000
            registers.PPUCTRL = 0;
            //PPUMASK ($2001)	0000 0000	0000 0000
            registers.PPUMASK = 0;
            //PPUSTATUS ($2002)	+0+x xxxx	U??x xxxx
            registers.PPUSTATUS = PPUStatus.VBlank | PPUStatus.SpriteOverflow;
            //OAMADDR ($2003)	$00	unchanged1
            registers.OAMADDR = 0;
            //PPUSCROLL ($2005)	$0000	$0000
            registers.PPUSCROLL = 0;
            //PPUADDR ($2006)	$0000	unchanged
            registers.PPUADDR = 0;
            //PPUDATA ($2007) read buffer	$00	$00
            registers.PPUDATA = 0;
            //odd frame	no	no
            //OAM	unspecified	unspecified
            //Palette	unspecified	unchanged
            //NT RAM (external, in Control Deck)	unspecified	unchanged
            //CHR RAM (external, in Game Pak)	unspecified	unchanged

            this.frameBufferHandle = FrameBuffer.Pin();
            this.pFrameBuffer = (uint*)frameBufferHandle.Pointer;
        }

        public void DoCycle()
        {
            if (scanline < 240)
            {
                Render();
            }

            if (scanline == 241)
            {
            }

            cycle++;

            if (cycle > 340)
            {
                cycle = 0;
                scanline++;

                if (scanline > 260)
                {
                    this.pFrameBuffer = (uint*)frameBufferHandle.Pointer;
                    scanline = 0;
                }
            }
        }

        private byte attributeTable = 0;
        private byte nameTable = 0;
        private byte bgTileLow = 0;
        private byte bgTileHigh = 0;
        private byte patternTableLow = 0;
        private byte patternTableHigh = 0;
        private Address nametableAddress = 0x2000;
        private Address patterntableAddress = 0x0000;
        private byte courseX = 0;
        private byte courseY = 0;

        private void Render()
        {
            //this.pn
            //this.pFrameBuffer*

            if (cycle == 0)
            {
                courseX = 0;
                //Console.WriteLine("PPU IDLE");
            }
            else if (1 <= cycle && cycle <= 256)
            {
                switch (cycle % 8)
                {
                    case 1:
                        var address = (Address)(((cycle / 8) * 16) + (scanline % 8) + ((scanline / 8) * 256));
                        bgTileLow = this.ppuBus.Read(address);
                        address = (Address)(((cycle / 8) * 16) + (scanline % 8) + ((scanline / 8) * 256) + 8);
                        bgTileHigh = this.ppuBus.Read(address);
                        //Console.WriteLine("PPU NT 1");
                        break;
                    case 2:
                        nametableAddress.Ptr = (ushort)(0x2000 | ((int)(registers.PPUCTRL & PPUControl.NametableMask) << 10) + courseX + scanline);
                        nameTable = this.ppuBus.Read(nametableAddress);
                        courseX++;
                        //Console.WriteLine("PPU NT 2");
                        break;
                    case 3:
                        //Console.WriteLine("PPU AT 1");
                        break;
                    case 4:
                        //Console.WriteLine("PPU AT 2");
                        break;
                    case 5:
                        //Console.WriteLine("PPU PTL 1");
                        break;
                    case 6:
                        patterntableAddress.Ptr = (ushort)(((registers.PPUCTRL & PPUControl.BGBank2) == 0 ? 0x0000 : 0x1000) + (nameTable << 4) + (scanline % 8));
                        patternTableLow = this.ppuBus.Read(patterntableAddress);
                        //Console.WriteLine("PPU PTL 2");
                        break;
                    case 7:
                        //Console.WriteLine("PPU PTH 1");
                        break;
                    case 0:
                        patterntableAddress.Ptr = (ushort)(((registers.PPUCTRL & PPUControl.BGBank2) == 0 ? 0x0000 : 0x1000) + (nameTable << 4) + (scanline % 8) + 8);
                        patternTableHigh = this.ppuBus.Read(patterntableAddress);
                        //Console.WriteLine("PPU PTH 2");
                        //bgTileLow = patternTableLow;
                        //bgTileHigh = patternTableHigh;
                        break;
                }

                *this.pFrameBuffer = (uint)(0x55555555 * ((bgTileLow & 0x80) >> 7 | (bgTileHigh & 0x80) >> 6)) | 0xFF000000;
                this.pFrameBuffer++;
                bgTileLow <<= 1;
                bgTileHigh <<= 1;
            }
            else if (257 <= cycle && cycle <= 320)
            {
                //Garbage nametable byte
                //Garbage nametable byte
                //Pattern table tile low
                //Pattern table tile high (+8 bytes from pattern table tile low)
            }
            else if (321 <= cycle && cycle <= 336)
            {
                //Nametable byte
                //Attribute table byte
                //Pattern table tile low
                //Pattern table tile high (+8 bytes from pattern table tile low)
            }
            else
            {
                //Nametable byte
                //Nametable byte
            }
        }

        public unsafe byte Read(Address address)
        {
            var offset = (address.Ptr % 8);
            byte value;

            switch (offset)
            {
                case 6:
                    throw new InvalidOperationException();
                case 7:
                    value = this.ppuBus.Read(this.address);
                    this.address.Ptr += (ushort)((this.registers.PPUCTRL & PPUControl.LineIncrement) == 0 ? 1 : 32);
                    return value;
                default:
                    fixed (PPURegisters* ptr = &registers)
                    {
                        value = *((byte*)ptr + (address.Ptr % 8));
                        return value;
                    }
            }
        }

        public unsafe void Write(Address address, byte value)
        {
            var offset = (address.Ptr % 8);

            switch (offset)
            {
                case 6:
                    this.address.High = this.address.Low;
                    this.address.Low = value;
                    break;
                case 7:
                    this.ppuBus.Write(this.address, value);
                    this.address.Ptr += (ushort)((this.registers.PPUCTRL & PPUControl.LineIncrement) == 0 ? 1 : 32);
                    break;
                default:
                    fixed (PPURegisters* ptr = &registers)
                    {
                        *((byte*)ptr + offset) = value;
                    }
                    break;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PPURegisters
    {
        [FieldOffset(0)]
        public PPUControl PPUCTRL;
        [FieldOffset(1)]
        public byte PPUMASK;
        [FieldOffset(2)]
        public PPUStatus PPUSTATUS;
        [FieldOffset(3)]
        public byte OAMADDR;
        [FieldOffset(4)]
        public byte OAMDATA;
        [FieldOffset(5)]
        public byte PPUSCROLL;
        [FieldOffset(6)]
        public byte PPUADDR;
        [FieldOffset(7)]
        public byte PPUDATA;
        //PPUCTRL	$2000	VPHB SINN	NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        //PPUMASK	$2001	BGRs bMmG	color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        //PPUSTATUS	$2002	VSO- ----	vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        //OAMADDR	$2003	aaaa aaaa	OAM read/write address
        //OAMDATA	$2004	dddd dddd	OAM data read/write
        //PPUSCROLL	$2005	xxxx xxxx	fine scroll position (two writes: X scroll, Y scroll)
        //PPUADDR	$2006	aaaa aaaa	PPU read/write address (two writes: most significant byte, least significant byte)
        //PPUDATA	$2007	dddd dddd	PPU data read/write
    }
}
