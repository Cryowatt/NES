using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional
    {
        public void ADC(byte operand)
        {
            var result = this.regs.A + operand + (byte)(this.regs.P & StatusFlags.Carry);
            this.regs.P = (this.regs.P & ~(StatusFlags.Overflow)) |
                (StatusFlags)(((this.regs.A ^ result) & (operand ^ result) & (int)StatusFlags.Negative) >> 1);
            this.regs.A = (byte)result;
            this.regs.Carry = (byte)(result > byte.MaxValue ? 1 : 0);
        }

        public void AND(byte operand)
        {
            this.regs.A &= operand;
        }

        public static byte ASL(Ricoh2AFunctional cpu, byte operand)
        {
            var result = operand << 1;
            cpu.regs.Carry = (byte)((result > 0xff) ? 1 : 0);
            //cpu.SetResultFlags((byte)result);
            return (byte)result;
        }

        public bool BCC() => this.regs.Carry == 0;
        public bool BCS() => this.regs.Carry == 1;
        public bool BEQ() => this.regs.Zero;
        public void BIT(byte operand)
        {
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative | StatusFlags.Overflow | StatusFlags.Zero)) |
                (((StatusFlags)operand) & (StatusFlags.Negative | StatusFlags.Overflow));

            if ((StatusFlags)(operand & this.regs.A) == 0)
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
            yield return cycleTrace;

            // 3  $0100,S  W  push PCH on stack (with B flag set), decrement S
            this.Write(this.Stack, this.regs.PC.High);
            this.regs.S--;
            yield return cycleTrace;

            // 4  $0100,S  W  push PCL on stack, decrement S
            this.Write(this.Stack, this.regs.PC.Low);
            this.regs.S--;
            yield return cycleTrace;

            // 5  $0100,S  W  push P on stack, decrement S
            this.Write(this.Stack, (byte)this.regs.P);
            this.regs.S--;
            yield return cycleTrace;

            // 6   $FFFE   R  fetch PCL
            this.regs.PC.Low = this.Read(0xfffe);
            yield return cycleTrace;

            // 7   $FFFF   R  fetch PCH
            this.regs.PC.High = this.Read(0xffff);
            yield return cycleTrace;

            TraceInstruction("BRK");
        }
        public bool BVC() => !this.regs.Overflow;
        public bool BVS() => this.regs.Overflow;
        public static void CLC(Ricoh2AFunctional cpu) => cpu.regs.Carry = 0;
        public static void CLD(Ricoh2AFunctional cpu) => cpu.regs.Decimal = false;
        public static void CLI(Ricoh2AFunctional cpu) => cpu.regs.InterruptDisable = false;
        public static void CLV(Ricoh2AFunctional cpu) => cpu.regs.Overflow = false;
        public void CMP(byte operand) => CMP(operand, this.regs.A);
        private void CMP(byte operand, byte register)
        {
            byte result = (byte)(register - operand);
            SetResultFlags(result);
            this.regs.Carry = (byte)(register >= operand ? 1 : 0);
        }
        public void CPX(byte operand) => CMP(operand, this.regs.X);
        public void CPY(byte operand) => CMP(operand, this.regs.Y);
        public byte DCP(byte operand)
        {
            operand--;
            CMP(operand);
            return operand;
        }
        public static byte DEC(Ricoh2AFunctional cpu, byte value)
        {
            value--;
            //SetResultFlags(value);
            return value;
        }
        public static void DEX(Ricoh2AFunctional cpu) => cpu.regs.X = DEC(cpu, cpu.regs.X);
        public static void DEY(Ricoh2AFunctional cpu) => cpu.regs.Y = DEC(cpu, cpu.regs.Y);
        public void EOR(byte operand)
        {
            this.regs.A ^= operand;
        }
        public byte INC(byte operand)
        {
            var result = (byte)(operand + 1);
            SetResultFlags(result);
            return result;
        }
        public void INX() => this.regs.X++;
        public void INY() => this.regs.Y++;
        public byte ISC(byte operand)
        {
            operand++;
            SBC(operand);
            return operand;
        }
        public static void JMP(Ricoh2AFunctional cpu) => cpu.regs.PC = cpu.address;
        public static void JSR(Ricoh2AFunctional cpu)
        {
            cpu.Enqueue(ReadPCToAddress);
            cpu.Enqueue(ReadStackNoOp);
            cpu.Enqueue(WriteStackFromAddressHigh);
            cpu.Enqueue(WriteStackFromAddressLow);
            cpu.Enqueue(c =>
            {
                ReadPCToAddressHigh(c);
                c.regs.PC = c.address;
                c.TraceInstruction("JSR", c.address);
            });
        }
        public void LAX(byte operand)
        {
            this.regs.A = operand;
            this.regs.X = operand;
        }
        public void LDA(byte operand) => this.regs.A = operand;
        public static void LDX(Ricoh2AFunctional cpu, byte operand) => cpu.regs.X = operand;
        public void LDY(byte operand) => this.regs.Y = operand;
        public static byte LSR(Ricoh2AFunctional cpu, byte operand)
        {
            cpu.regs.Carry = (byte)(operand & 0x01);
            var result = (byte)(operand >> 1);
            //SetResultFlags(result);
            return result;
        }
        public static void NOP(Ricoh2AFunctional cpu) { }
        public static void NOP(Ricoh2AFunctional cpu, byte operand) { }
        public void ORA(byte operand)
        {
            this.regs.A |= operand;
        }
        public IEnumerable<object> PHA() => PushValue(this.regs.A);
        public IEnumerable<object> PHP() => PushValue((byte)(this.regs.P | StatusFlags.Default));
        public IEnumerable<object> PLA() => PullValue(v => this.regs.A = v);
        public IEnumerable<object> PLP() => PullValue(v => this.regs.P = (StatusFlags)v);
        public static byte ROL(Ricoh2AFunctional cpu, byte operand)
        {
            int result = (operand << 1) | cpu.regs.Carry;
            cpu.regs.Carry = (byte)((result > 0xff) ? 1 : 0);
            //SetResultFlags((byte)result);
            return (byte)result;
        }
        public byte RLA(byte operand)
        {
            throw new NotImplementedException();
            //var result = ROL(operand);
            //AND(result);
            //return result;
        }
        public static byte ROR(Ricoh2AFunctional cpu, byte operand)
        {
            int result = operand | (cpu.regs.Carry << 8);
            cpu.regs.Carry = (byte)(operand & 0x1);
            result >>= 1;
            //SetResultFlags((byte)result);
            return (byte)result;
        }
        public byte RRA(byte operand)
        {
            throw new NotImplementedException();
            //var result = ROR(operand);
            //ADC(result);
            //return result;
        }
        public IEnumerable<object> RTI()
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return cycleTrace;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            this.regs.S++;
            yield return cycleTrace;

            // 4  $0100,S  R  pull P from stack, increment S
            this.regs.P = (StatusFlags)this.Read(this.Stack);
            this.regs.S++;
            yield return cycleTrace;

            // 5  $0100,S  R  pull PCL from stack, increment S
            this.regs.PC.Low = this.Read(this.Stack);
            this.regs.S++;
            yield return cycleTrace;

            // 6  $0100,S  R  pull PCH from stack
            this.regs.PC.High = this.Read(this.Stack);
            yield return cycleTrace;
            TraceInstruction("RTI");
        }
        public IEnumerable<object> RTS()
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return cycleTrace;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            checked
            {
                this.regs.S++;
                yield return cycleTrace;

                // 4  $0100,S  R  pull PCL from stack, increment S
                this.regs.PC.Low = this.Read(this.Stack);
                this.regs.S++;
                yield return cycleTrace;
            }

            // 5  $0100,S  R  pull PCH from stack
            this.regs.PC.High = this.Read(this.Stack);
            yield return cycleTrace;

            // 6    PC     R  increment PC
            this.Read(this.regs.PC++);
            yield return cycleTrace;
            TraceInstruction("RTS");
        }
        public byte SAX() => (byte)(this.regs.A & this.regs.X);
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
        public static void SEC(Ricoh2AFunctional cpu) => cpu.regs.P |= StatusFlags.Carry;
        public static void SED(Ricoh2AFunctional cpu) => cpu.regs.P |= StatusFlags.Decimal;
        public static void SEI(Ricoh2AFunctional cpu) { cpu.regs.InterruptDisable = true; }
        public byte SLO(byte operand)
        {
            throw new NotFiniteNumberException();
            //var result = ASL(operand);
            //ORA(result);
            //return result;
        }
        public byte SRE(byte operand)
        {
            throw new NotFiniteNumberException();
            //var result = LSR(operand);
            //EOR(result);
            //return result;
        }
        public byte STA() { return this.regs.A; }
        public static byte STX(Ricoh2AFunctional cpu) => cpu.regs.X;
        public byte STY() { return this.regs.Y; }
        public static void TAX(Ricoh2AFunctional cpu) => cpu.regs.X = cpu.regs.A;
        public void TAY() => this.regs.Y = this.regs.A;
        public void TSX() => this.regs.X = this.regs.S;
        public static void TXA(Ricoh2AFunctional cpu) => cpu.regs.A = cpu.regs.X;
        public void TXS() => this.regs.S = this.regs.X;
        public static void TYA(Ricoh2AFunctional cpu) => cpu.regs.A = cpu.regs.Y;

        private IEnumerable<object> PushValue(byte value, [CallerMemberName] string caller = null)
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return cycleTrace;

            // 3  $0100,S  W  push register on stack, decrement S
            this.Write(this.Stack, value);
            this.regs.S--;
            yield return cycleTrace;
            TraceInstruction(caller);
        }

        private IEnumerable<object> PullValue(Action<byte> setter, [CallerMemberName] string caller = null)
        {
            // 2    PC     R  read next instruction byte (and throw it away)
            this.Read(this.regs.PC);
            yield return cycleTrace;

            // 3  $0100,S  R  increment S
            this.Read(this.Stack);
            this.regs.S++;
            yield return cycleTrace;

            // 4  $0100,S  R  pull register from stack
            var value = this.Read(this.Stack);
            setter(value);
            yield return cycleTrace;
            TraceInstruction(caller);
        }

        private void SetResultFlags(byte result)
        {
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative | StatusFlags.Zero)) |
                (((StatusFlags)result) & StatusFlags.Negative) |
                (result == 0 ? StatusFlags.Zero : (StatusFlags)0);
        }
    }
}
