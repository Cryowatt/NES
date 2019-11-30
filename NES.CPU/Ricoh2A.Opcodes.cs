using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
            SetResultFlags(this.regs.A);
        }

        public byte ASL(byte operand)
        {
            var result = operand << 1;
            if (result > 0xff)
            {
                this.regs.Carry = 1;
            }
            SetResultFlags((byte)result);
            return (byte)result;
        }

        public bool BCC() => this.regs.Carry == 0;
        public bool BCS() => this.regs.Carry == 1;
        public bool BEQ() => this.regs.Zero;
        public void BIT(byte operand)
        {
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative | StatusFlags.Overflow | StatusFlags.Zero)) |
                (((StatusFlags)operand) & (StatusFlags.Negative | StatusFlags.Overflow));

            if ((StatusFlags)(operand & this.regs.A) != 0)
            {
                this.regs.P |= StatusFlags.Zero;
            }
        }
        public bool BMI() => this.regs.Negative;
        public bool BNE() => !this.regs.Zero;
        public bool BPL() => !this.regs.Negative;
        public IEnumerable<object> BRK()
        {
            // 2    PC     R  read next instruction byte (and throw it away),
            //                increment PC
            this.Read(this.regs.PC++);
            Debug.WriteLine("0x{0:X4} BRK", this.regs.PC - 1);
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
        public void CMP(byte operand) => CMP(operand, this.regs.A);
        private void CMP(byte operand, byte register)
        {
            byte result = (byte)(register - operand);
            SetResultFlags(result);
            this.regs.Carry = (byte)(register >= operand ? 1 : 0);
        }
        public void CPX(byte operand) => CMP(operand, this.regs.X);
        public void CPY(byte operand) => CMP(operand, this.regs.Y);
        public byte DEC(byte value)
        {
            value--;
            SetResultFlags(value);
            return value;
        }
        public void DEX() => this.regs.X = DEC(this.regs.X);
        public void DEY() => this.regs.Y = DEC(this.regs.Y);
        public void EOR() => throw new NotImplementedException();
        public byte INC(byte operand)
        {
            var result = (byte)(operand + 1);
            SetResultFlags(result);
            return result;
        }
        public void INX() => MOV((byte)(this.regs.X + 1), ref this.regs.X);
        public void INY() => MOV((byte)(this.regs.Y + 1), ref this.regs.Y);
        public void JMP(Address address) { this.regs.PC = address; }
        public IEnumerable<object> JSR()
        {
            // 2    PC     R  fetch low address byte, increment PC
            Address address = this.Read(this.regs.PC++);
            yield return null;

            // 3  $0100,S  R  internal operation (predecrement S?)
            this.Read(this.Stack);
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
            address.High = this.Read(this.regs.PC);
            Debug.WriteLine("0x{0:X4} JSR #{1}", this.regs.PC - 1, address);
            this.regs.PC = address;
            yield return null;
        }
        public void LDA(byte operand) => MOV(operand, ref this.regs.A);
        public void LDX(byte operand) => MOV(operand, ref this.regs.X);
        public void LDY(byte operand) => MOV(operand, ref this.regs.Y);
        public byte LSR(byte operand)
        {
            this.regs.Carry = (byte)(operand & 0x01);
            var result = operand >> 1;
            return (byte)result;
        }
        public void NOP() { }
        public void ORA(byte operand)
        {
            this.regs.A &= operand;
            SetResultFlags(this.regs.A);
        }
        public IEnumerable<object> PHA() => PushValue(this.regs.A);
        public IEnumerable<object> PHP() => PushValue((byte)this.regs.P);
        public IEnumerable<object> PLA() => PullValue(v => this.regs.A = v);
        public IEnumerable<object> PLP() => PullValue(v => this.regs.P = (StatusFlags)v);
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
            Debug.WriteLine("0x{0:X4} RTI", this.regs.PC - 1);
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
            Debug.WriteLine("0x{0:X4} RTS", this.regs.PC - 1);
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
        public void SBC(byte operand) => ADC((byte)~operand);
        //{
        //    var result = this.regs.A - operand - (byte)(this.regs.P & StatusFlags.Carry);
        //    this.regs.P = (this.regs.P & ~(StatusFlags.Overflow | StatusFlags.Negative)) |
        //        (((StatusFlags)result) & StatusFlags.Negative) |
        //        (StatusFlags)(((this.regs.A ^ result) & (operand ^ result) & (int)StatusFlags.Negative) >> 1);
        //    this.regs.A = (byte)result;
        //    this.regs.Carry = (byte)(result < 0 ? 1 : 0);
        //    this.regs.Zero = this.regs.A == 0;
        //}
        public void SEC() => throw new NotImplementedException();
        public void SED() => throw new NotImplementedException();
        public void SEI() { this.regs.InterruptDisable = true; }
        public byte STA() { return this.regs.A; }
        public byte STX() { return this.regs.X; }
        public byte STY() { return this.regs.Y; }
        public void TAX() => MOV(this.regs.A, ref this.regs.X);
        public void TAY() => MOV(this.regs.A, ref this.regs.Y);
        public void TSX() => MOV(this.regs.S, ref this.regs.X);
        public void TXA() => MOV(this.regs.X, ref this.regs.A);
        public void TXS() => MOV(this.regs.X, ref this.regs.S);
        public void TYA() => MOV(this.regs.Y, ref this.regs.A);

        private void MOV(byte value, ref byte destination)
        {
            destination = value;
            SetResultFlags(destination);
        }
        private IEnumerable<object> PushValue(byte value, [CallerMemberName] string caller = null)
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            Debug.WriteLine("0x{0:X4} {1} #${2}", this.regs.PC - 1, caller, value);
            yield return null;

            // 3  $0100,S  W  push register on stack, decrement S
            this.Write(this.Stack, value);
            this.regs.S--;
            yield return null;
        }

        private IEnumerable<object> PullValue(Action<byte> setter, [CallerMemberName] string caller = null)
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            Debug.WriteLine("0x{0:X4} {1}", this.regs.PC - 1, caller);
            yield return null;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            this.regs.S++;
            yield return null;

            // 4  $0100,S  R  pull register from stack
            setter(this.Read(this.Stack));
            yield return null;
        }

        private void SetResultFlags(byte result)
        {
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative | StatusFlags.Zero)) |
                (((StatusFlags)result) & StatusFlags.Negative) |
                (result == 0 ? StatusFlags.Zero : (StatusFlags)0);
        }
    }
}
