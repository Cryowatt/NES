using System;
using System.Collections;
using System.Collections.Generic;

namespace NES.CPU
{
    public class Ricoh2A
    {
        private IBus bus;

        public Ricoh2A(IBus bus) : this(bus, new CpuRegisters()) { }

        public Ricoh2A(IBus bus, CpuRegisters registers)
        {
            this.bus = bus;
            this.regs = registers;
        }

        public CpuRegisters Registers => regs;

        private CpuRegisters regs;

        public IEnumerable<object> Process()
        {
            this.regs.PCL = Read(0xFFFC);
            yield return null;
            this.regs.PCH = Read(0xFFFC);
            yield return null;

            while (true)
            {
                // PC:R  fetch opcode, increment PC
                byte opcode = Read(this.regs.PC++);
                yield return null;

                IEnumerable<object> instructionCycles = opcode switch
                {
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0a  ASL     ROL     LSR     ROR     TXA     TAX     DEX     NOP     Accu/imp
                    0x0a => AccumulatorAddressing(ASL),
                    0x2a => AccumulatorAddressing(ROL),
                    0x4a => AccumulatorAddressing(LSR),
                    0x6a => AccumulatorAddressing(ROR),
                    0x8a => ImpliedAddressing(TXA),
                    0xaa => ImpliedAddressing(TAX),
                    0xca => ImpliedAddressing(DEX),
                    0xea => ImpliedAddressing(NOP),
                    _ => throw new NotImplementedException()
                    //ADC(operand);
                };

                foreach (var cycle in instructionCycles)
                {
                    yield return cycle;
                }
            }
        }

        private IEnumerable<object> AccumulatorAddressing(Func<byte, byte> microcode)
        {
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            yield return null;
            this.regs.A = microcode(this.regs.A);
        }

        private IEnumerable<object> ImpliedAddressing(Action microcode)
        {
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            yield return null;
            microcode();
        }

        public byte ADC(byte operand)
        {
            var result = this.regs.A + operand + (byte)(this.regs.P & StatusFlags.Carry);
            this.regs.Zero = result == 0;
            return (byte)result;
        }

        public byte AND(byte operand)
        {
            var result = this.regs.A & operand;
            this.regs.Zero = result == 0;
            return (byte)result;
        }

        public byte ASL(byte operand)
        {
            var result = operand << 1;
            if (result > 0xff)
            {
                this.regs.Carry = 1;
            }
            return (byte)result;
        }

        public void BCC(byte operand) { }
        public void BCS(byte operand) { }
        public void BEQ(byte operand) { }
        public void BIT() { }
        public void BMI() { }
        public void BNE() { }
        public void BPL() { }
        public void BRK() { }
        public void BVC() { }
        public void BVS() { }
        public void CLC() { }
        public void CLS() { }
        public void CLD() { }
        public void CLI() { }
        public void CLV() { }
        public void CMP() { }
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
        public void JMP() { }
        public void JSR() { }
        public void LDA() { }
        public void LDX() { }

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
        public void SEI() { }
        public void STA() { }
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

        //private bool FlagNegative
        //{
        //    get
        //    {

        //    }
        //    set { }
        //}

        public IEnumerable<object> ADC()
        {
            //             #  address R/W description
            //--- ------- --- ------------------------------------------
            // 1    PC     R  fetch opcode, increment PC
            // 2    PC     R  fetch low byte of address, increment PC
            // 3    PC     R  fetch high byte of address, increment PC
            // 4  address  R  read from effective address
            byte opcode = Read(this.regs.PC++);
            yield return null;
            Read(this.regs.PC++);
        }

        private byte Read(Address address)
        {
            return 0;
        }
    }
}
