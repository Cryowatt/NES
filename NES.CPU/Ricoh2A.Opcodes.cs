namespace NES.CPU
{
    public partial class Ricoh2A
    {
        public void ADC(byte operand)
        {
            var result = this.regs.A + operand + (byte)(this.regs.P & StatusFlags.Carry);
            this.regs.P = (this.regs.P & ~(StatusFlags.Overflow | StatusFlags.Negative)) |
                (((StatusFlags)result) & StatusFlags.Negative) |
                (StatusFlags)(((this.regs.A ^ result) & (operand ^ result) & (int)StatusFlags.Negative) >> 1);
            this.regs.A = (byte)result;
            this.regs.Carry = (byte)(result > byte.MaxValue ? 1 : 0);
            this.regs.Zero = this.regs.A == 0;
        }

        public void AND(byte operand)
        {
            this.regs.A &= operand;
            this.regs.Zero = this.regs.A == 0;
            this.regs.P = (this.regs.P & ~StatusFlags.Negative) | ((StatusFlags)this.regs.A & StatusFlags.Negative);
        }

        public byte ASL(byte operand)
        {
            var result = operand << 1;
            if (result > 0xff)
            {
                this.regs.Carry = 1;
            }
            this.regs.P = (this.regs.P & ~StatusFlags.Negative) |
                (((StatusFlags)result) & StatusFlags.Negative);
            this.regs.Zero = (byte)result == 0;
            return (byte)result;
        }

        public bool BCC() => this.regs.Carry == 0;
        public bool BCS() => this.regs.Carry == 1;
        public bool BEQ() => this.regs.Zero;
        public void BIT(byte operand)
        {
            const StatusFlags mask = StatusFlags.Negative | StatusFlags.Overflow;
            this.regs.P = (this.regs.P & ~mask) | (((StatusFlags)operand) & mask);
            this.regs.Zero = (operand & this.regs.A) == 0;
        }
        public bool BMI() => this.regs.Negative;
        public bool BNE() => !this.regs.Zero;
        public bool BPL() => !this.regs.Negative;
        public void BRK() { }
        public bool BVC() => !this.regs.Overflow;
        public bool BVS() => this.regs.Overflow;
        public void CLC() => this.regs.Carry = 0;
        public void CLD() => this.regs.Decimal = false;
        public void CLI() => this.regs.InterruptDisable = false;
        public void CLV() => this.regs.Overflow = false;
        public void CMP(byte operand)
        {
            var result = this.regs.A - operand;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)result) & StatusFlags.Negative);
            this.regs.Carry = (byte)(result > byte.MaxValue ? 1 : 0);
            this.regs.Zero = (byte)result == 0;
        }
        public void CPX() { }
        public void CPY() { }
        public void DEC() { }

        public void DEX()
        {
            this.regs.X--;
        }

        public void DEY() { }
        public void EOR() { }
        public void INC() { }
        public void INX() { }
        public void INY() { }
        public void JMP(Address address) { this.regs.PC = address; }
        public void JSR() { }
        public void LDA(byte operand) { this.regs.A = operand; }
        public void LDX() { }
        public void LDY() { }

        public byte LSR(byte operand)
        {
            this.regs.Carry = (byte)(operand & 0x01);
            var result = operand >> 1;
            return (byte)result;
        }
        public void NOP() { }
        public void ORA() { }
        public void PHA() { }
        public void PHP() { }
        public void PLA() { }
        public void PLP() { }
        public byte ROL(byte operand)
        {
            int result = (operand << 1) | this.regs.Carry;
            if (result > 0xff)
            {
                this.regs.Carry = 1;
            }

            return (byte)result;
        }

        public byte ROR(byte operand)
        {
            int result = operand | (this.regs.Carry << 8);
            this.regs.Carry = (byte)(operand & 0x1);
            result >>= 1;

            return (byte)result;
        }

        public void RTI() { }
        public void RTS() { }
        public void SBC() { }
        public void SEC() { }
        public void SED() { }
        public void SEI() { this.regs.InterruptDisable = true; }
        public byte STA() { return this.regs.A; }
        public void STX() { }
        public void STY() { }
        public void TAX()
        {
            this.regs.X = this.regs.A;
        }

        public void TAY() { }
        public void TSX() { }

        public void TXA()
        {
            this.regs.A = this.regs.X;
        }

        public void TXS() { }
        public void TYA() { }
    }
}
