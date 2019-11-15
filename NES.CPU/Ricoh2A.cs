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
                    //+00  BRK     JSR     RTI     RTS     NOP*    LDY     CPY     CPX     Impl/immed
                    0x00 => StubAddressing(BRK),
                    0x20 => StubAddressing(JSR),
                    0x40 => StubAddressing(RTI),
                    0x60 => StubAddressing(RTS),
                    0x80 => StubAddressing(NOP), //*
                    0xa0 => StubAddressing(LDY),
                    0xc0 => StubAddressing(CPY),
                    0xe0 => StubAddressing(CPX),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+01  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir,x)
                    0x01 => StubAddressing(ORA),
                    0x21 => StubAddressing(AND),
                    0x41 => StubAddressing(EOR),
                    0x61 => StubAddressing(ADC),
                    0x81 => StubAddressing(STA),
                    0xa1 => StubAddressing(LDA),
                    0xc1 => StubAddressing(CMP),
                    0xe1 => StubAddressing(SBC),
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+02   t       t       t       t      NOP*t   LDX     NOP*t   NOP*t     ? /immed
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+04  NOP*    BIT     NOP*    NOP*    STY     LDY     CPY     CPX     Zeropage
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+05  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+06  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Zeropage
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+08  PHP     PLP     PHA     PLA     DEY     TAY     INY     INX     Implied
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+09  ORA     AND     EOR     ADC     NOP*    LDA     CMP     SBC     Immediate
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
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0b  ANC**   ANC**   ASR**   ARR**   ANE**   LXA**   SBX**   SBC*    Immediate
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0c  NOP*    BIT     JMP     JMP ()  STY     LDY     CPY     CPX     Absolute
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0e  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Absolute
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+10  BPL     BMI     BVC     BVS     BCC     BCS     BNE     BEQ     Relative
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+12   t       t       t       t       t       t       t       t         ?
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+13  SLO*    RLA*    SRE*    RRA*    SHA**   LAX*    DCP*    ISB*    (indir),y
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+15  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+18  CLC     SEC     CLI     SEI     TYA     CLV     CLD     SED     Implied
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1a  NOP*    NOP*    NOP*    NOP*    TXS     TSX     NOP*    NOP*    Implied
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x

                    _ => throw new NotImplementedException()
                };

                foreach (var cycle in instructionCycles)
                {
                    yield return cycle;
                }
            }
        }

        // This will be deleted later
        private IEnumerable<object> StubAddressing(Action microcode)
        {
            throw new NotImplementedException();
        }
        private IEnumerable<object> StubAddressing(Func<byte, byte> microcode)
        {
            throw new NotImplementedException();
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
