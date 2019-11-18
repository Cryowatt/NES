using System;
using System.Collections;
using System.Collections.Generic;

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

        public void AHX() { }

        public void ANC(byte operand) => throw new InvalidOperationException("Invalid Opcode");

        public void AND(byte operand)
        {
            this.regs.A &= operand;
            this.regs.Zero = this.regs.A == 0;
            this.regs.P = (this.regs.P & ~StatusFlags.Negative) | ((StatusFlags)this.regs.A & StatusFlags.Negative);
        }

        public void ARR() => throw new InvalidOperationException("Invalid Opcode");
        
        public byte ASL(byte operand)
        {
            var result = operand << 1;
            if (result > 0xff)
            {
                this.regs.Carry = 1;
            }
            this.regs.P = (this.regs.P & ~StatusFlags.Negative) |
                (((StatusFlags)result) & StatusFlags.Negative);
            this.regs.Zero = result == 0;
            return (byte)result;
        }

        public void ALR() { }
        public void AXS() { }
        public void BCC() { }
        public void BCS() { }
        public void BEQ() { }
        public void BIT() { }
        public void BMI() { }
        public void BNE() { }
        public void BPL() { }
        public void BRK() { }
        public void BVC() { }
        public void BVS() { }
        public void CLC() { }
        public void CLD() { }
        public void CLI() { }
        public void CLV() { }
        public void CMP() { }
        public void CPX() { }
        public void CPY() { }
        public void DCP() { }
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
        public void ISC() { }
        public void JMP() { }
        public void JSR() { }
        public void LAS() { }
        public void LAX() { }
        public void LDA() { }
        public void LDX() { }
        public void LDY() { }


        public byte LSR(byte operand)
        {
            this.regs.Carry = (byte)(operand & 0x01);
            var result = operand >> 1;
            return (byte)result;
        }
        public void LXA() { }
        public void NOP() { }
        public void ORA() { }
        public void PHA() { }
        public void PHP() { }
        public void PLA() { }
        public void PLP() { }
        public void RLA() { }
        public void RRA() { }
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
        public void SAX() { }
        public void SBC() { }
        public void SBX() { }
        public void SEC() { }
        public void SED() { }
        public void SEI() { }
        public void SHA() { }
        public void SHX() { }
        public void SHS() { }

        public void SHY() { }
        public void SLO() { }
        public void SRE() { }
        public void STA() { }
        public void STX() { }
        public void STY() { }

        public void TAS() { }
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
        public void XAA() => throw new InvalidOperationException("Invalid Opcode");
    }
}
