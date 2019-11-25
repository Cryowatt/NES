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
        public IEnumerable<object> BRK()
        {
            // 2    PC     R  read next instruction byte (and throw it away),
            //                increment PC
            this.Read(this.regs.PC++);
            yield return null;

            // 3  $0100,S  W  push PCH on stack (with B flag set), decrement S
            this.Write(this.Stack, this.regs.PC.High);
            this.regs.S--;
            yield return null;

            // 4  $0100,S  W  push PCL on stack, decrement S
            this.Write(this.Stack, this.regs.PC.Low);
            this.regs.S--;
            yield return null;

            // 5  $0100,S  W  push P on stack, decrement S
            this.Write(this.Stack, (byte)this.regs.P);
            this.regs.S--;
            yield return null;

            // 6   $FFFE   R  fetch PCL
            this.regs.PC.Low = this.Read(0xfffe);
            yield return null;

            // 7   $FFFF   R  fetch PCH
            this.regs.PC.High = this.Read(0xffff);
            yield return null;
        }
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
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.X) & StatusFlags.Negative);
            this.regs.Zero = this.regs.X == 0;
        }

        public void DEY() { }
        public void EOR() { }
        public byte INC(byte operand)
        {
            var result = operand + 1;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)result) & StatusFlags.Negative);
            this.regs.Zero = result == 0;
            return (byte)result;
        }
        public void INX()
        {
            this.regs.X += 1;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.X) & StatusFlags.Negative);
            this.regs.Zero = this.regs.X == 0;
        }
        public void INY()
        {
            this.regs.Y += 1;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.Y) & StatusFlags.Negative);
            this.regs.Zero = this.regs.Y == 0;
        }
        public void JMP(Address address) { this.regs.PC = address; }
        public IEnumerable<object> JSR()
        {
            // 2    PC     R  fetch low address byte, increment PC
            Address addrss = this.Read(this.regs.PC++);
            yield return null;

            // 3  $0100,S  R  internal operation (predecrement S?)
            this.Read(this.Stack);
            this.regs.S--;
            yield return null;

            // 4  $0100,S  W  push PCH on stack, decrement S
            this.Write(this.Stack, this.regs.PC.High);
            this.regs.S--;
            yield return null;

            // 5  $0100,S  W  push PCL on stack, decrement S
            this.Write(this.Stack, this.regs.PC.Low);
            this.regs.S--;
            yield return null;

            // 6    PC     R  copy low address byte to PCL, fetch high address
            //                byte to PCH
            addrss.High = this.Read(this.regs.PC);
            this.regs.PC = addrss;
            yield return null;
        }
        public void LDA(byte operand)
        {
            this.regs.A = operand;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.A) & StatusFlags.Negative);
            this.regs.Zero = this.regs.A == 0;
        }
        public void LDX(byte operand)
        {
            this.regs.X = operand;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.X) & StatusFlags.Negative);
            this.regs.Zero = this.regs.X == 0;
        }
        public void LDY(byte operand)
        {
            this.regs.Y = operand;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.Y) & StatusFlags.Negative);
            this.regs.Zero = this.regs.Y == 0;
        }
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

        public IEnumerable<object> RTI()
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return null;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 4  $0100,S  R  pull P from stack, increment S
            this.regs.P = (StatusFlags)this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 5  $0100,S  R  pull PCL from stack, increment S
            this.regs.PC.Low = this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 6  $0100,S  R  pull PCH from stack
            this.regs.PC.High = this.Read(this.Stack);
            yield return null;
        }
        public IEnumerable<object> RTS()
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return null;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 4  $0100,S  R  pull PCL from stack, increment S
            this.regs.PC.Low = this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 5  $0100,S  R  pull PCH from stack
            this.regs.PC.High = this.Read(this.Stack);
            yield return null;

            // 6    PC     R  increment PC
            this.Read(this.regs.PC++);
            yield return null;
        }
        public void SBC() { }
        public void SEC() { }
        public void SED() { }
        public void SEI() { this.regs.InterruptDisable = true; }
        public byte STA() { return this.regs.A; }
        public byte STX() { return this.regs.X; }
        public byte STY() { return this.regs.Y; }
        public void TAX()
        {
            this.regs.X = this.regs.A;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.X) & StatusFlags.Negative);
            this.regs.Zero = this.regs.X == 0;
        }
        public void TAY() { }
        public void TSX()
        {
            this.regs.X = this.regs.S;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.X) & StatusFlags.Negative);
            this.regs.Zero = this.regs.X == 0;
        }
        public void TXA()
        {
            this.regs.A = this.regs.X;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.A) & StatusFlags.Negative);
            this.regs.Zero = this.regs.A == 0;
        }
        public void TXS()
        {
            this.regs.S = this.regs.X;
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative)) |
                (((StatusFlags)this.regs.S) & StatusFlags.Negative);
            this.regs.Zero = this.regs.S == 0;
        }
        public void TYA() { }
    }
}
