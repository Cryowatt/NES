using System;
using System.Collections;
using System.Collections.Generic;

namespace NES.CPU
{
    public class Ricoh2A
    {
        private byte A;
        private byte X;
        private byte Y;
        private byte P;

        private ushort SP;
        private ushort PC;

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

        //private bool FlagNegative
        //{
        //    get
        //    {

        //    }
        //    set { }
        //}

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
