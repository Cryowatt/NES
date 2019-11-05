using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct CpuRegisters
        {
            [FieldOffset(0)]
            public ushort PC;
            [FieldOffset(0)]
            public byte PCL;
            [FieldOffset(1)]
            public byte PCH;
            [FieldOffset(2)]
            public byte S;
            [FieldOffset(3)]
            public byte A;
            [FieldOffset(4)]
            public byte X;
            [FieldOffset(5)]
            public byte Y;
            [FieldOffset(6)]
            public byte P;
        }

        private CpuRegisters Registers = new CpuRegisters();

        //7  bit  0
        //---- ----
        //NVss DIZC
        //|||| ||||
        //|||| |||+- Carry
        //|||| ||+-- Zero
        //|||| |+--- Interrupt Disable
        //|||| +---- Decimal
        //||++------ No CPU effect, see: the B flag
        //|+-------- Overflow
        //+--------- Negative
        [Flags]
        public enum StatusFlags : byte
        {
            Carry = 0x1 << 0,
            Zero = 0x1 << 1,
            InterruptDisable = 0x1 << 2,
            Decimal = 0x1 << 3,
            Undefined_5 = 0x1 << 4,
            Undefined_6 = 0x1 << 5,
            Overflow = 0x1 << 6,
            Negative = 0x1 << 7,
        }

        private bool FlagCarry
        {
            get
            {
                return (this.P & (byte)StatusFlags.Carry) > 0;
            }
            set
            {
                if (value)
                {
                    this.P |= (byte)StatusFlags.Carry;
                }
                else
                {
                    this.P &= (byte)~StatusFlags.Carry;
                }
            }
        }

        public IEnumerable<object> Process()
        {
            byte opcode = Read(this.Registers.PC++);
            yield return null;

            switch (opcode)
            {
                default:
                    ADC()
            }
        }
        //private bool FlagNegative
        //{
        //    get
        //    {

        //    }
        //    set { }
        //}

        public IEnumerable<object> ADC()
        {
            //             #  address R/W description
            //--- ------- --- ------------------------------------------
            // 1    PC     R  fetch opcode, increment PC
            // 2    PC     R  fetch low byte of address, increment PC
            // 3    PC     R  fetch high byte of address, increment PC
            // 4  address  R  read from effective address
            byte opcode = Read(this.Registers.PC++);
            Read(this.Registers.PC++);
        }

        private byte Read(ushort address)
        {
            return 0;
        }

        public IEnumerable BRK()
        {
            //             #  address R/W description
            //--- ------- --- -----------------------------------------------
            // 1    PC     R  fetch opcode, increment PC

            this.PC++;
            yield return null;
            // 2    PC     R  read next instruction byte (and throw it away),
            //                increment PC
            yield return null;
            // 3  $0100,S  W  push PCH on stack (with B flag set), decrement S
            yield return null;
            // 4  $0100,S  W  push PCL on stack, decrement S
            yield return null;
            // 5  $0100,S  W  push P on stack, decrement S
            yield return null;
            // 6   $FFFE   R  fetch PCL
            yield return null;
            // 7   $FFFF   R  fetch PCH
            yield return null;
        }
    }
}
