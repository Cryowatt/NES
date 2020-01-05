using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        Mask = 0b1110_0000,
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

    public struct PPUAddress
    {
        private const int CoarseXMask = 0b11111;
        private const int CoarseYShift = 5;
        private const int CoarseYMask = 0b11111 << CoarseYShift;
        private const int NametableShift = CoarseYShift + 5;
        private const int NametableMask = 0b11 << NametableShift;
        private const int FineYShift = NametableShift + 2;
        private const int FineYMask = 0b111 << FineYShift;

        public Address Address;

        public ushort Ptr
        {
            get => this.Address.Ptr;
            set => this.Address.Ptr = value;
        }

        public byte Low
        {
            get => this.Address.Low;
            set => this.Address.Low = value;
        }

        public byte High
        {
            get => this.Address.High;
            set => this.Address.High = value;
        }

        // yyy NN YYYYY XXXXX
        // ||| || ||||| +++++-- coarse X scroll
        // ||| || +++++-------- coarse Y scroll
        // ||| ++-------------- nametable select
        // +++----------------- fine Y scroll

        public byte CoarseX
        {
            get => Get(CoarseXMask);
            set => Set(value, CoarseXMask);
        }

        public byte CoarseY
        {
            get => Get(CoarseYMask, CoarseYShift);
            set => Set(value, CoarseYMask, CoarseYShift);
        }

        public byte Nametable
        {
            get => Get(NametableMask, NametableShift);
            set => Set(value, NametableMask, NametableShift);
        }

        public byte FineY
        {
            get => Get(FineYMask, FineYShift);
            set => Set(value, FineYMask, FineYShift);
        }

        private byte Get(int mask, int offset = 0) =>
            (byte)((this.Address.Ptr & mask) >> offset);

        private void Set(byte value, int mask, int offset = 0) =>
            this.Address.Ptr = (ushort)((this.Address.Ptr & ~mask) | ((value << offset) & mask));
    }

    public unsafe class PPU : IBusDevice
    {
        private int cycle = 0;
        private int scanline = 0;
        private PPURegisters registers;
        private PPUBus ppuBus;
        private MemoryHandle frameBufferHandle;
        private uint* pFrameBuffer;
        //private Address patterntableAddress = 0x0000;

        // Loopy registers
        private PPUAddress vAddress;
        private PPUAddress tAddress;
        private byte fineX;
        private bool writeHigh;
        private byte dataBuffer;

        public Memory<int> FrameBuffer { get; }
        public ReadOnlyMemory<byte> Nametable => this.ppuBus.Nametable;
        public ReadOnlyMemory<byte> PatternTable => this.ppuBus.PatternTable;

        public PPURegisters Registers => registers;

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
            //else if (scanline == 241 && cycle == 1)
            //{
            //    this.registers.PPUSTATUS |= PPUStatus.VBlank;
            //}

            cycle++;
            if (cycle > 340)
            {
                cycle = 0;
                scanline++;

                if (scanline == 241)
                {
                    this.registers.PPUSTATUS |= PPUStatus.VBlank;
                }
                if (scanline > 260)
                {
                    this.pFrameBuffer = (uint*)frameBufferHandle.Pointer;
                    scanline = 0;
                }
            }
        }

        private void Render()
        {
            if (cycle == 256)
            {
                // TODO: If rendering is enabled, the PPU increments the vertical position in v. The effective Y scroll coordinate is incremented, which is a complex operation that will correctly skip the attribute table memory regions, and wrap to the next nametable appropriately.

                if (this.vAddress.FineY < 7) // if fine Y < 7
                {
                    this.vAddress.FineY++; // increment fine Y
                }
                else
                {
                    this.vAddress.FineY = 0;
                    byte y = this.vAddress.CoarseY;

                    if (y == 29)
                    {
                        y = 0; // coarse Y = 0
                        this.vAddress.Nametable ^= 2; // switch vertical nametable
                    }
                    else if (y == 31)
                    {
                        y = 0; // coarse Y = 0, nametable not switched
                    }
                    else
                    {
                        y += 1; // increment coarse Y
                    }

                    this.vAddress.CoarseY = y; // put coarse Y back into v
                }
            }
            else if (cycle == 257)
            {
                // yyy NN YYYYY XXXXX
                // ||| || ||||| +++++-- coarse X scroll
                // ||| || +++++-------- coarse Y scroll
                // ||| ++-------------- nametable select
                // +++----------------- fine Y scroll

                // v: ....F.. ...EDCBA = t: ....F.. ...EDCBA
                const ushort mask = 0b000_0100_0001_1111;
                this.vAddress.Ptr = (ushort)((this.vAddress.Ptr & ~mask) | (this.tAddress.Ptr & mask));
            }
            else if (280 <= cycle && cycle <= 304)
            {
                // v: IHGF.ED CBA..... = t: IHGF.ED CBA.....
                const ushort mask = 0b111_1011_1110_0000;
                this.vAddress.Ptr = (ushort)((this.vAddress.Ptr & ~mask) | (this.tAddress.Ptr & mask));
            }

            //if (cycle == 0)
            //{
            //    courseX = 0;
            //    //Console.WriteLine("PPU IDLE");
            //}
            //else if (1 <= cycle && cycle <= 256)
            //{
            //    switch (cycle % 8)
            //    {
            //        case 1:
            //            var address = (Address)(((cycle / 8) * 16) + (scanline % 8) + ((scanline / 8) * 256));
            //            bgTileLow = this.ppuBus.Read(address);
            //            address = (Address)(((cycle / 8) * 16) + (scanline % 8) + ((scanline / 8) * 256) + 8);
            //            bgTileHigh = this.ppuBus.Read(address);
            //            //Console.WriteLine("PPU NT 1");
            //            break;
            //        case 2:
            //            nametableAddress.Ptr = (ushort)(0x2000 | ((int)(Registers.PPUCTRL & PPUControl.NametableMask) << 10) + courseX + scanline);
            //            nameTable = this.ppuBus.Read(nametableAddress);
            //            courseX++;
            //            //Console.WriteLine("PPU NT 2");
            //            break;
            //        case 3:
            //            //Console.WriteLine("PPU AT 1");
            //            break;
            //        case 4:
            //            //Console.WriteLine("PPU AT 2");
            //            break;
            //        case 5:
            //            //Console.WriteLine("PPU PTL 1");
            //            break;
            //        case 6:
            //            patterntableAddress.Ptr = (ushort)(((Registers.PPUCTRL & PPUControl.BGBank2) == 0 ? 0x0000 : 0x1000) + (nameTable << 4) + (scanline % 8));
            //            patternTableLow = this.ppuBus.Read(patterntableAddress);
            //            //Console.WriteLine("PPU PTL 2");
            //            break;
            //        case 7:
            //            //Console.WriteLine("PPU PTH 1");
            //            break;
            //        case 0:
            //            patterntableAddress.Ptr = (ushort)(((Registers.PPUCTRL & PPUControl.BGBank2) == 0 ? 0x0000 : 0x1000) + (nameTable << 4) + (scanline % 8) + 8);
            //            patternTableHigh = this.ppuBus.Read(patterntableAddress);
            //            //Console.WriteLine("PPU PTH 2");
            //            //bgTileLow = patternTableLow;
            //            //bgTileHigh = patternTableHigh;
            //            break;
            //    }

            //    *this.pFrameBuffer = (uint)(0x55555555 * ((bgTileLow & 0x80) >> 7 | (bgTileHigh & 0x80) >> 6)) | 0xFF000000;
            //    this.pFrameBuffer++;
            //    bgTileLow <<= 1;
            //    bgTileHigh <<= 1;
            //}
            //else if (257 <= cycle && cycle <= 320)
            //{
            //    //Garbage nametable byte
            //    //Garbage nametable byte
            //    //Pattern table tile low
            //    //Pattern table tile high (+8 bytes from pattern table tile low)
            //}
            //else if (321 <= cycle && cycle <= 336)
            //{
            //    //Nametable byte
            //    //Attribute table byte
            //    //Pattern table tile low
            //    //Pattern table tile high (+8 bytes from pattern table tile low)
            //}
            //else
            //{
            //    //Nametable byte
            //    //Nametable byte
            //}
        }

        public unsafe byte Read(Address address)
        {
            byte data;

            switch (address.Ptr & 0x7)
            {
                case 2:
                    data = (byte)((this.registers.PPUSTATUS & PPUStatus.Mask) | ((PPUStatus)this.dataBuffer & ~PPUStatus.Mask));
                    this.registers.PPUSTATUS &= ~PPUStatus.VBlank;
                    this.writeHigh = false;
                    return data;
                case 7:
                    data = this.dataBuffer;
                    this.dataBuffer = this.ppuBus.Read(this.vAddress.Address);

                    // Don't buffer palette reads
                    if (this.vAddress.Ptr >= 0x3F00)
                    {
                        data = this.dataBuffer;
                    }
                    this.vAddress.Ptr += (ushort)((this.registers.PPUCTRL & PPUControl.LineIncrement) == 0 ? 1 : 32);
                    return data;
                default:
                    return 0;
            }
        }

        public unsafe void Write(Address address, byte value)
        {
            switch (address.Ptr & 0x7)
            {
                case 0:
                    // t: ...BA.. ........ = d: ......BA
                    this.registers.PPUCTRL = (PPUControl)value;
                    this.tAddress.Nametable = value;
                    break;
                case 1:
                    this.registers.PPUMASK = value;
                    break;
                case 5:
                    if (this.writeHigh)
                    {
                        // t: CBA..HG FED..... = d: HGFEDCBA
                        // w:                  = 0
                        this.tAddress.FineY = (byte)value;
                        this.tAddress.CoarseY = (byte)(value >> 3);
                        this.writeHigh = false;
                    }
                    else
                    {
                        // t: ....... ...HGFED = d: HGFED...
                        // x: CBA = d: .....CBA
                        // w:                  = 1
                        this.fineX = value;
                        this.tAddress.CoarseX = (byte)(value >> 3);
                        this.writeHigh = true;
                    }
                    break;
                case 6:
                    if (this.writeHigh)
                    {
                        // $2006 second write(w is 1)
                        // t: ....... HGFEDCBA = d: HGFEDCBA
                        // v = t
                        // w:                  = 0
                        this.tAddress.Low = value;
                        this.vAddress.Ptr = this.tAddress.Ptr;
                        this.writeHigh = false;
                    }
                    else
                    {
                        // $2006 first write(w is 0)
                        // t: .FEDCBA........ = d: ..FEDCBA
                        // t: X.............. = 0
                        // w:                  = 1
                        this.tAddress.High = (byte)(value & 0b111111);
                        this.writeHigh = true;
                    }
                    break;
                case 7:
                    if (0x40 <= value && value < 0x80)
                    {
                        Console.Write((char)value);
                    }
                    this.ppuBus.Write(this.vAddress.Address, value);
                    this.vAddress.Ptr += (ushort)((this.registers.PPUCTRL & PPUControl.LineIncrement) == 0 ? 1 : 32);
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
