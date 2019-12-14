using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional
    {
        public static void ADC(Ricoh2AFunctional cpu, byte operand)
        {
            var result = cpu.regs.A + operand + (byte)(cpu.regs.P & StatusFlags.Carry);
            cpu.regs.P = (cpu.regs.P & ~(StatusFlags.Overflow | StatusFlags.Carry)) |
                (StatusFlags)(((cpu.regs.A ^ result) & (operand ^ result) & (int)StatusFlags.Negative) >> 1) |
                (result > byte.MaxValue ? StatusFlags.Carry : 0);
            cpu.regs.A = (byte)result;
        }

        public static void AND(Ricoh2AFunctional cpu, byte operand) => cpu.regs.A &= operand;
        public static byte ASL(Ricoh2AFunctional cpu, byte operand)
        {
            var result = operand << 1;
            cpu.regs.Carry = (byte)((result > 0xff) ? 1 : 0);
            cpu.SetResultFlags((byte)result);
            return (byte)result;
        }

        public static bool BCC(Ricoh2AFunctional cpu) => cpu.regs.Carry == 0;
        public static bool BCS(Ricoh2AFunctional cpu) => cpu.regs.Carry == 1;
        public static bool BEQ(Ricoh2AFunctional cpu) => cpu.regs.Zero;
        public static void BIT(Ricoh2AFunctional cpu, byte operand)
        {
            cpu.regs.P = (cpu.regs.P & ~(StatusFlags.Negative | StatusFlags.Overflow | StatusFlags.Zero)) |
                (((StatusFlags)operand) & (StatusFlags.Negative | StatusFlags.Overflow));

            if ((StatusFlags)(operand & cpu.regs.A) == 0)
            {
                cpu.regs.P |= StatusFlags.Zero;
            }
        }
        public static bool BMI(Ricoh2AFunctional cpu) => cpu.regs.Negative;
        public static bool BNE(Ricoh2AFunctional cpu) => !cpu.regs.Zero;
        public static bool BPL(Ricoh2AFunctional cpu) => !cpu.regs.Negative;
        public void BRK()
        {
            Enqueue(c => c.Read(c.regs.PC.Ptr++));
            Enqueue(PushStackFromPCH);
            Enqueue(PushStackFromPCL);
            Enqueue(PushStackFromP);
            Enqueue(c => c.regs.PC.Low = c.Read(0xfffe));
            Enqueue(c =>
            {
                c.regs.PC.High = c.Read(0xffff);
                c.TraceInstruction("BRK");
            });
        }
        public static bool BVC(Ricoh2AFunctional cpu) => !cpu.regs.Overflow;
        public static bool BVS(Ricoh2AFunctional cpu) => cpu.regs.Overflow;
        public static void CLC(Ricoh2AFunctional cpu) => cpu.regs.Carry = 0;
        public static void CLD(Ricoh2AFunctional cpu) => cpu.regs.Decimal = false;
        public static void CLI(Ricoh2AFunctional cpu) => cpu.regs.InterruptDisable = false;
        public static void CLV(Ricoh2AFunctional cpu) => cpu.regs.Overflow = false;
        public static void CMP(Ricoh2AFunctional cpu, byte operand) => CMP(cpu, operand, cpu.regs.A);
        private static void CMP(Ricoh2AFunctional cpu, byte operand, byte register)
        {
            byte result = (byte)(register - operand);
            cpu.SetResultFlags(result);
            cpu.regs.Carry = (byte)(register >= operand ? 1 : 0);
        }
        public static void CPX(Ricoh2AFunctional cpu, byte operand) => CMP(cpu, operand, cpu.regs.X);
        public static void CPY(Ricoh2AFunctional cpu, byte operand) => CMP(cpu, operand, cpu.regs.Y);
        public static byte DCP(Ricoh2AFunctional cpu, byte operand)
        {
            operand--;
            CMP(cpu, operand);
            return operand;
        }
        public static byte DEC(Ricoh2AFunctional cpu, byte value)
        {
            value--;
            cpu.SetResultFlags(value);
            return value;
        }
        public static void DEX(Ricoh2AFunctional cpu) => cpu.regs.X = DEC(cpu, cpu.regs.X);
        public static void DEY(Ricoh2AFunctional cpu) => cpu.regs.Y = DEC(cpu, cpu.regs.Y);
        public static void EOR(Ricoh2AFunctional cpu, byte operand) => cpu.regs.A ^= operand;
        public static byte INC(Ricoh2AFunctional cpu, byte operand)
        {
            var result = (byte)(operand + 1);
            cpu.SetResultFlags(result);
            return result;
        }
        public static void INX(Ricoh2AFunctional cpu) => cpu.regs.X++;
        public static void INY(Ricoh2AFunctional cpu) => cpu.regs.Y++;
        public static byte ISC(Ricoh2AFunctional cpu, byte operand)
        {
            operand++;
            SBC(cpu, operand);
            return operand;
        }
        public static void JMP(Ricoh2AFunctional cpu) => cpu.regs.PC = cpu.address;
        public void JSR()
        {
            static void ReadHighAndJump(Ricoh2AFunctional cpu)
            {
                cpu.address.High = cpu.Read(cpu.regs.PC);
                cpu.regs.PC = cpu.address;
                cpu.TraceInstruction("JSR", cpu.address);
            }

            Enqueue(ReadPCToAddress);
            Enqueue(ReadStackNoOp);
            Enqueue(PushStackFromPCH);
            Enqueue(PushStackFromPCL);
            Enqueue(ReadHighAndJump);
        }
        public static void LAX(Ricoh2AFunctional cpu, byte operand)
        {
            cpu.regs.A = operand;
            cpu.regs.X = operand;
        }
        public static void LDA(Ricoh2AFunctional cpu, byte operand) => cpu.regs.A = operand;
        public static void LDX(Ricoh2AFunctional cpu, byte operand) => cpu.regs.X = operand;
        public static void LDY(Ricoh2AFunctional cpu, byte operand) => cpu.regs.Y = operand;
        public static byte LSR(Ricoh2AFunctional cpu, byte operand)
        {
            cpu.regs.Carry = (byte)(operand & 0x01);
            var result = (byte)(operand >> 1);
            cpu.SetResultFlags(result);
            return result;
        }
        public static void NOP(Ricoh2AFunctional cpu) { }
        public static void NOP(Ricoh2AFunctional cpu, byte operand) { }
        public static void ORA(Ricoh2AFunctional cpu, byte operand) => cpu.regs.A |= operand;
        public void PHA()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Write(c.Stack, c.regs.A);
                c.regs.S--;
                c.TraceInstruction("PHA");
            });
        }
        public void PHP()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Write(c.Stack, (byte)(c.regs.P | StatusFlags.Default));
                c.regs.S--;
                c.TraceInstruction("PHP");
            });
        }
        public void PLA()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.A = c.Read(c.Stack);
                c.TraceInstruction("PLA");
            });
        }
        public void PLP()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.P = (StatusFlags)c.Read(c.Stack);
                c.TraceInstruction("PLA");
            });
        }
        public static byte ROL(Ricoh2AFunctional cpu, byte operand)
        {
            int result = (operand << 1) | cpu.regs.Carry;
            cpu.regs.Carry = (byte)((result > 0xff) ? 1 : 0);
            cpu.SetResultFlags((byte)result);

            return (byte)result;
        }
        public static byte RLA(Ricoh2AFunctional cpu, byte operand)
        {
            var result = ROL(cpu, operand);
            AND(cpu, result);
            return result;
        }
        public static byte ROR(Ricoh2AFunctional cpu, byte operand)
        {
            int result = operand | (cpu.regs.Carry << 8);
            cpu.regs.Carry = (byte)(operand & 0x1);
            result >>= 1;
            cpu.SetResultFlags((byte)result);
            return (byte)result;
        }
        public static byte RRA(Ricoh2AFunctional cpu, byte operand)
        {
            var result = ROR(cpu, operand);
            ADC(cpu, result);
            return result;
        }
        public void RTI()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.P = (StatusFlags)c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.PC.Low = c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.PC.High = c.Read(c.Stack);
                c.TraceInstruction("RTI");
            });
        }
        public void RTS()
        {
            Enqueue(ReadPCNoOp);
            Enqueue(c =>
            {
                c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.PC.Low = c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.regs.S++;
                c.regs.PC.High = c.Read(c.Stack);
            });
            Enqueue(c =>
            {
                c.Read(c.regs.PC++);
                c.TraceInstruction("RTS", c.regs.PC);
            });
        }
        public static byte SAX(Ricoh2AFunctional cpu) => (byte)(cpu.regs.A & cpu.regs.X);
        public static void SBC(Ricoh2AFunctional cpu, byte operand) => ADC(cpu, (byte)~operand);
        public static void SEC(Ricoh2AFunctional cpu) => cpu.regs.P |= StatusFlags.Carry;
        public static void SED(Ricoh2AFunctional cpu) => cpu.regs.P |= StatusFlags.Decimal;
        public static void SEI(Ricoh2AFunctional cpu) { cpu.regs.InterruptDisable = true; }
        public static byte SLO(Ricoh2AFunctional cpu, byte operand)
        {
            var result = ASL(cpu, operand);
            ORA(cpu, result);
            return result;
        }
        public static byte SRE(Ricoh2AFunctional cpu, byte operand)
        {
            var result = LSR(cpu, operand);
            EOR(cpu, result);
            return result;
        }
        public static byte STA(Ricoh2AFunctional cpu) => cpu.regs.A;
        public static byte STX(Ricoh2AFunctional cpu) => cpu.regs.X;
        public static byte STY(Ricoh2AFunctional cpu) => cpu.regs.Y;
        public static void TAX(Ricoh2AFunctional cpu) => cpu.regs.X = cpu.regs.A;
        public static void TAY(Ricoh2AFunctional cpu) => cpu.regs.Y = cpu.regs.A;
        public static void TSX(Ricoh2AFunctional cpu) => cpu.regs.X = cpu.regs.S;
        public static void TXA(Ricoh2AFunctional cpu) => cpu.regs.A = cpu.regs.X;
        public static void TXS(Ricoh2AFunctional cpu) => cpu.regs.S = cpu.regs.X;
        public static void TYA(Ricoh2AFunctional cpu) => cpu.regs.A = cpu.regs.Y;

        private void SetResultFlags(byte result)
        {
            this.regs.P = (this.regs.P & ~(StatusFlags.Negative | StatusFlags.Zero)) |
                (((StatusFlags)result) & StatusFlags.Negative) |
                (result == 0 ? StatusFlags.Zero : (StatusFlags)0);
        }
    }
}
