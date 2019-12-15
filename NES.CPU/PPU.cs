using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NES.CPU
{
    public class PPU : IBusDevice
    {
        private PPURegisters registers;

        public PPU()
        {
            //PPUCTRL ($2000)	0000 0000	0000 0000
            registers.PPUCTRL = 0;
            //PPUMASK ($2001)	0000 0000	0000 0000
            registers.PPUMASK = 0;
            //PPUSTATUS ($2002)	+0+x xxxx	U??x xxxx
            registers.PPUSTATUS = 0b1010_0000;
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
        }

        public void DoCycle()
        {

        }

        public unsafe byte Read(Address address)
        {
            if (0x2000 <= address.Ptr && address.Ptr < 0x4000)
            {
                fixed (PPURegisters* ptr = &registers)
                {
                    var value = *((byte*)ptr + (address.Ptr % 8));
                    Console.WriteLine($"PPU {address} => {value}");
                    return value;
                }
            }

            throw new NotImplementedException();
        }

        public unsafe void Write(Address address, byte value)
        {
            fixed (PPURegisters* ptr = &registers)
            {
                Console.WriteLine($"PPU {address} <= {value}");
                *((byte*)ptr + (address.Ptr % 8)) = value;
                return;
            }

            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PPURegisters
    {
        [FieldOffset(0)]
        public byte PPUCTRL;
        [FieldOffset(1)]
        public byte PPUMASK;
        [FieldOffset(2)]
        public byte PPUSTATUS;
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
