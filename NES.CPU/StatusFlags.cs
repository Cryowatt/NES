using System;

namespace NES.CPU
{
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
        Default = Undefined_5 | Undefined_6,
        None = 0,
        Carry = 0x1 << 0,
        Zero = 0x1 << 1,
        InterruptDisable = 0x1 << 2,
        Decimal = 0x1 << 3,
        Undefined_5 = 0x1 << 4,
        Undefined_6 = 0x1 << 5,
        Overflow = 0x1 << 6,
        Negative = 0x1 << 7,
    }
}
