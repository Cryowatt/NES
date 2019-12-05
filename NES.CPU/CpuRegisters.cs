using System;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    [StructLayout(LayoutKind.Explicit)]
    public struct CpuRegisters : ICloneable
    {
        [FieldOffset(0)]
        public Address PC;
        [FieldOffset(2)]
        public byte S;
        [FieldOffset(3)]
        private byte a;
        [FieldOffset(4)]
        private byte x;
        [FieldOffset(5)]
        private byte y;
        [FieldOffset(6)]
        private StatusFlags p;

        public CpuRegisters(StatusFlags flags) : this()
        {
            this.S = 0xfd;
            this.PC = 0x0000;
            this.P = flags;
        }

        public override string ToString() => $"A[{A:X2}] X[{X:X2}] Y[{Y:X2}] PC[{PC}] S[{S:X2}] P[{(byte)P:X2} {Convert.ToString((byte)P, 2)} {P}]";

        public byte Carry
        {
            get => (byte)(this.P & StatusFlags.Carry);
            set => this.P = (this.P & ~StatusFlags.Carry) | ((StatusFlags)value & StatusFlags.Carry);
        }

        public bool Zero
        {
            get => GetFlag(StatusFlags.Zero);
            set => SetFlag(StatusFlags.Zero, value);
        }

        public bool InterruptDisable
        {
            get => GetFlag(StatusFlags.InterruptDisable);
            set => SetFlag(StatusFlags.InterruptDisable, value);
        }

        public bool Decimal
        {
            get => GetFlag(StatusFlags.Decimal);
            set => SetFlag(StatusFlags.Decimal, value);
        }

        public bool Overflow
        {
            get => GetFlag(StatusFlags.Overflow);
            set => SetFlag(StatusFlags.Overflow, value);
        }

        public bool Negative
        {
            get => GetFlag(StatusFlags.Negative);
            set => SetFlag(StatusFlags.Negative, value);
        }

        private void SetResultFlags(byte result)
        {
            this.P = (this.P & ~(StatusFlags.Negative | StatusFlags.Zero)) |
                (((StatusFlags)result) & StatusFlags.Negative) |
                (result == 0 ? StatusFlags.Zero : (StatusFlags)0);
        }

        public byte A
        {
            get
            {
                return this.a;
            }

            set
            {
                this.a = value;
                SetResultFlags(value);
            }
        }

        public byte X
        {
            get
            {
                return this.x;
            }

            set
            {
                this.x = value;
                SetResultFlags(value);
            }
        }

        public byte Y
        {
            get
            {
                return this.y;
            }

            set
            {
                this.y = value;
                SetResultFlags(value);
            }
        }

        public StatusFlags P
        {
            get
            {
                return this.p;
            }

            set
            {
                this.p = (value | StatusFlags.Undefined_6) & ~StatusFlags.Undefined_5;
            }
        }

        private bool GetFlag(StatusFlags flag) => this.P.HasFlag(flag);

        private void SetFlag(StatusFlags flag, bool isEnabled)
        {
            if (isEnabled)
            {
                this.P |= flag;
            }
            else
            {
                this.P &= ~flag;
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public static readonly CpuRegisters Empty = new CpuRegisters(StatusFlags.Default);
    }
}
