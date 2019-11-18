using System.Runtime.InteropServices;

namespace NES.CPU
{
    [StructLayout(LayoutKind.Explicit)]
    public struct CpuRegisters
    {
        [FieldOffset(0)]
        public Address PC;
        [FieldOffset(2)]
        public byte S;
        [FieldOffset(3)]
        public byte A;
        [FieldOffset(4)]
        public byte X;
        [FieldOffset(5)]
        public byte Y;
        [FieldOffset(6)]
        public StatusFlags P;

        public CpuRegisters(StatusFlags flags)
        {
            this.S = 0xfd;
            this.A = this.X = this.Y = 0;
            this.PC = 0x0000;
            this.P = flags;
        }

        public override string ToString() => $"PC[{PC}] S[{S}] A[{A}] X[{X}] Y[{Y}] P[{P}]";

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

        public static readonly CpuRegisters Empty = new CpuRegisters(StatusFlags.Default);
    }
}
