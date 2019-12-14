using System;
using System.Collections.Generic;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional
    {
        private void QueueOpCode()
        {
            if (this.InstructionTrace != null && this.lastInstruction != null)
            {
                this.InstructionTrace(this.lastInstruction);
            }

            this.currentOpcodeAddress.Ptr = this.regs.PC.Ptr;
            this.currentOpcode = this.Read(this.regs.PC);
            this.regs.PC.Ptr++;

            switch (this.currentOpcode)
            {
                //set  00      20      40      60      80      a0      c0      e0      mode
                //+00  BRK     JSR     RTI     RTS     NOP*    LDY     CPY     CPX     Impl/immed
                case 0x00: BRK(); break;
                case 0x20: JSR(); break;
                case 0x40: RTI(); break;
                case 0x60: RTS(); break;
                case 0x80: ImmediateAddressing(NOP); break;
                case 0xa0: ImmediateAddressing(LDY); break;
                case 0xc0: ImmediateAddressing(CPY); break;
                case 0xe0: ImmediateAddressing(CPX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+01  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir,x)
                case 0x01: IndexedIndirectAddressing(ORA); break;
                case 0x21: IndexedIndirectAddressing(AND); break;
                case 0x41: IndexedIndirectAddressing(EOR); break;
                case 0x61: IndexedIndirectAddressing(ADC); break;
                case 0x81: IndexedIndirectAddressing(STA); break;
                case 0xa1: IndexedIndirectAddressing(LDA); break;
                case 0xc1: IndexedIndirectAddressing(CMP); break;
                case 0xe1: IndexedIndirectAddressing(SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+02   t       t       t       t      NOP*t   LDX     NOP*t   NOP*t     ? /immed
                case 0xa2: ImmediateAddressing(LDX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                case 0x03: IndexedIndirectAddressing(SLO); break;
                case 0x23: IndexedIndirectAddressing(RLA); break;
                case 0x43: IndexedIndirectAddressing(SRE); break;
                case 0x63: IndexedIndirectAddressing(RRA); break;
                case 0x83: IndexedIndirectAddressing(SAX); break;
                case 0xa3: IndexedIndirectAddressing(LAX); break;
                case 0xc3: IndexedIndirectAddressing(DCP); break;
                case 0xe3: IndexedIndirectAddressing(ISC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+04  NOP*    BIT     NOP*    NOP*    STY     LDY     CPY     CPX     Zeropage
                case 0x04: ZeroPageAddressing(NOP); break;
                case 0x24: ZeroPageAddressing(BIT); break;
                case 0x44: ZeroPageAddressing(NOP); break;
                case 0x64: ZeroPageAddressing(NOP); break;
                case 0x84: ZeroPageAddressing(STY); break;
                case 0xa4: ZeroPageAddressing(LDY); break;
                case 0xc4: ZeroPageAddressing(CPY); break;
                case 0xe4: ZeroPageAddressing(CPX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+05  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage
                case 0x05: ZeroPageAddressing(ORA); break;
                case 0x25: ZeroPageAddressing(AND); break;
                case 0x45: ZeroPageAddressing(EOR); break;
                case 0x65: ZeroPageAddressing(ADC); break;
                case 0x85: ZeroPageAddressing(STA); break;
                case 0xa5: ZeroPageAddressing(LDA); break;
                case 0xc5: ZeroPageAddressing(CMP); break;
                case 0xe5: ZeroPageAddressing(SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+06  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Zeropage
                case 0x06: ZeroPageAddressing(ASL); break;
                case 0x26: ZeroPageAddressing(ROL); break;
                case 0x46: ZeroPageAddressing(LSR); break;
                case 0x66: ZeroPageAddressing(ROR); break;
                case 0x86: ZeroPageAddressing(STX); break;
                case 0xa6: ZeroPageAddressing(LDX); break;
                case 0xc6: ZeroPageAddressing(DEC); break;
                case 0xe6: ZeroPageAddressing(INC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage
                case 0x07: ZeroPageAddressing(SLO); break;
                case 0x27: ZeroPageAddressing(RLA); break;
                case 0x47: ZeroPageAddressing(SRE); break;
                case 0x67: ZeroPageAddressing(RRA); break;
                case 0x87: ZeroPageAddressing(SAX); break;
                case 0xa7: ZeroPageAddressing(LAX); break;
                case 0xc7: ZeroPageAddressing(DCP); break;
                case 0xe7: ZeroPageAddressing(ISC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+08  PHP     PLP     PHA     PLA     DEY     TAY     INY     INX     Implied
                case 0x08: PHP(); break;
                case 0x28: PLP(); break;
                case 0x48: PHA(); break;
                case 0x68: PLA(); break;
                case 0x88: ImpliedAddressing(DEY); break;
                case 0xa8: ImpliedAddressing(TAY); break;
                case 0xc8: ImpliedAddressing(INY); break;
                case 0xe8: ImpliedAddressing(INX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+09  ORA     AND     EOR     ADC     NOP*    LDA     CMP     SBC     Immediate
                case 0x09: ImmediateAddressing(ORA); break;
                case 0x29: ImmediateAddressing(AND); break;
                case 0x49: ImmediateAddressing(EOR); break;
                case 0x69: ImmediateAddressing(ADC); break;
                case 0xa9: ImmediateAddressing(LDA); break;
                case 0xc9: ImmediateAddressing(CMP); break;
                case 0xe9: ImmediateAddressing(SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0a  ASL     ROL     LSR     ROR     TXA     TAX     DEX     NOP     Accu/imp
                case 0x0a: AccumulatorAddressing(ASL); break;
                case 0x2a: AccumulatorAddressing(ROL); break;
                case 0x4a: AccumulatorAddressing(LSR); break;
                case 0x6a: AccumulatorAddressing(ROR); break;
                case 0x8a: ImpliedAddressing(TXA); break;
                case 0xaa: ImpliedAddressing(TAX); break;
                case 0xca: ImpliedAddressing(DEX); break;
                case 0xea: ImpliedAddressing(NOP); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0b  ANC**   ANC**   ASR**   ARR**   ANE**   LXA**   SBX**   SBC*    Immediate
                case 0xeb: ImmediateAddressing(SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0c  NOP*    BIT     JMP     JMP ()  STY     LDY     CPY     CPX     Absolute
                case 0x0c: AbsoluteAddressing((Action<Ricoh2AFunctional, byte>)NOP); break;
                case 0x2c: AbsoluteAddressing(BIT); break;
                case 0x4c: AbsoluteAddressing(JMP); break;
                case 0x6c: AbsoluteIndirectAddressing(JMP); break;
                case 0x8c: AbsoluteAddressing(STY); break;
                case 0xac: AbsoluteAddressing(LDY); break;
                case 0xcc: AbsoluteAddressing(CPY); break;
                case 0xec: AbsoluteAddressing(CPX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute
                case 0x0d: AbsoluteAddressing(ORA); break;
                case 0x2d: AbsoluteAddressing(AND); break;
                case 0x4d: AbsoluteAddressing(EOR); break;
                case 0x6d: AbsoluteAddressing(ADC); break;
                case 0x8d: AbsoluteAddressing(STA); break;
                case 0xad: AbsoluteAddressing(LDA); break;
                case 0xcd: AbsoluteAddressing(CMP); break;
                case 0xed: AbsoluteAddressing(SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0e  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Absolute
                case 0x0e: AbsoluteAddressing(ASL); break;
                case 0x2e: AbsoluteAddressing(ROL); break;
                case 0x4e: AbsoluteAddressing(LSR); break;
                case 0x6e: AbsoluteAddressing(ROR); break;
                case 0x8e: AbsoluteAddressing(STX); break;
                case 0xae: AbsoluteAddressing(LDX); break;
                case 0xce: AbsoluteAddressing(DEC); break;
                case 0xee: AbsoluteAddressing(INC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute
                case 0x0f: AbsoluteAddressing(SLO); break;
                case 0x2f: AbsoluteAddressing(RLA); break;
                case 0x4f: AbsoluteAddressing(SRE); break;
                case 0x6f: AbsoluteAddressing(RRA); break;
                case 0x8f: AbsoluteAddressing(SAX); break;
                case 0xaf: AbsoluteAddressing(LAX); break;
                case 0xcf: AbsoluteAddressing(DCP); break;
                case 0xef: AbsoluteAddressing(ISC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+10  BPL     BMI     BVC     BVS     BCC     BCS     BNE     BEQ     Relative
                case 0x10: RelativeAddressing(BPL); break;
                case 0x30: RelativeAddressing(BMI); break;
                case 0x50: RelativeAddressing(BVC); break;
                case 0x70: RelativeAddressing(BVS); break;
                case 0x90: RelativeAddressing(BCC); break;
                case 0xb0: RelativeAddressing(BCS); break;
                case 0xd0: RelativeAddressing(BNE); break;
                case 0xf0: RelativeAddressing(BEQ); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                case 0x11: IndirectIndexedAddressing(ORA); break;
                case 0x31: IndirectIndexedAddressing(AND); break;
                case 0x51: IndirectIndexedAddressing(EOR); break;
                case 0x71: IndirectIndexedAddressing(ADC); break;
                case 0x91: IndirectIndexedAddressing(STA); break;
                case 0xb1: IndirectIndexedAddressing(LDA); break;
                case 0xd1: IndirectIndexedAddressing(CMP); break;
                case 0xf1: IndirectIndexedAddressing(SBC); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+12   t       t       t       t       t       t       t       t         ?
                ////0x12 => StubAddressing(STP),
                ////0x32 => StubAddressing(STP),
                ////0x52 => StubAddressing(STP),
                ////0x72 => StubAddressing(STP),
                ////0x92 => StubAddressing(STP),
                ////0xb2 => StubAddressing(STP),
                ////0xd2 => StubAddressing(STP),
                ////0xf2 => StubAddressing(STP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+13  SLO*    RLA*    SRE*    RRA*    SHA**   LAX*    DCP*    ISB*    (indir),y
                case 0x13: IndirectIndexedAddressing(SLO); break;
                case 0x33: IndirectIndexedAddressing(RLA); break;
                case 0x53: IndirectIndexedAddressing(SRE); break;
                case 0x73: IndirectIndexedAddressing(RRA); break;
                case 0xb3: IndirectIndexedAddressing(LAX); break;
                case 0xd3: IndirectIndexedAddressing(DCP); break;
                case 0xf3: IndirectIndexedAddressing(ISC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                case 0x14: ZeroPageIndexedAddressing(NOP, this.regs.X); break;
                case 0x34: ZeroPageIndexedAddressing(NOP, this.regs.X); break;
                case 0x54: ZeroPageIndexedAddressing(NOP, this.regs.X); break;
                case 0x74: ZeroPageIndexedAddressing(NOP, this.regs.X); break;
                case 0x94: ZeroPageIndexedAddressing(STY, this.regs.X); break;
                case 0xb4: ZeroPageIndexedAddressing(LDY, this.regs.X); break;
                case 0xd4: ZeroPageIndexedAddressing(NOP, this.regs.X); break;
                case 0xf4: ZeroPageIndexedAddressing(NOP, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+15  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage,x
                case 0x15: ZeroPageIndexedAddressing(ORA, this.regs.X); break;
                case 0x35: ZeroPageIndexedAddressing(AND, this.regs.X); break;
                case 0x55: ZeroPageIndexedAddressing(EOR, this.regs.X); break;
                case 0x75: ZeroPageIndexedAddressing(ADC, this.regs.X); break;
                case 0x95: ZeroPageIndexedAddressing(STA, this.regs.X); break;
                case 0xb5: ZeroPageIndexedAddressing(LDA, this.regs.X); break;
                case 0xd5: ZeroPageIndexedAddressing(CMP, this.regs.X); break;
                case 0xf5: ZeroPageIndexedAddressing(SBC, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                case 0x16: ZeroPageIndexedAddressing(ASL, this.regs.X); break;
                case 0x36: ZeroPageIndexedAddressing(ROL, this.regs.X); break;
                case 0x56: ZeroPageIndexedAddressing(LSR, this.regs.X); break;
                case 0x76: ZeroPageIndexedAddressing(ROR, this.regs.X); break;
                case 0x96: ZeroPageIndexedAddressing(STX, this.regs.Y); break;
                case 0xb6: ZeroPageIndexedAddressing(LDX, this.regs.Y); break;
                case 0xd6: ZeroPageIndexedAddressing(DEC, this.regs.X); break;
                case 0xf6: ZeroPageIndexedAddressing(INC, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x
                case 0x17: ZeroPageIndexedAddressing(SLO, this.regs.X); break;
                case 0x37: ZeroPageIndexedAddressing(RLA, this.regs.X); break;
                case 0x57: ZeroPageIndexedAddressing(SRE, this.regs.X); break;
                case 0x77: ZeroPageIndexedAddressing(RRA, this.regs.X); break;
                case 0x97: ZeroPageIndexedAddressing(SAX, this.regs.Y); break;
                case 0xb7: ZeroPageIndexedAddressing(LAX, this.regs.Y); break;
                case 0xd7: ZeroPageIndexedAddressing(DCP, this.regs.X); break;
                case 0xf7: ZeroPageIndexedAddressing(ISC, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+18  CLC     SEC     CLI     SEI     TYA     CLV     CLD     SED     Implied
                case 0x18: ImpliedAddressing(CLC); break;
                case 0x38: ImpliedAddressing(SEC); break;
                case 0x58: ImpliedAddressing(CLI); break;
                case 0x78: ImpliedAddressing(SEI); break;
                case 0x98: ImpliedAddressing(TYA); break;
                case 0xb8: ImpliedAddressing(CLV); break;
                case 0xd8: ImpliedAddressing(CLD); break;
                case 0xf8: ImpliedAddressing(SED); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                case 0x19: AbsoluteIndexedAddressing(ORA, this.regs.Y); break;
                case 0x39: AbsoluteIndexedAddressing(AND, this.regs.Y); break;
                case 0x59: AbsoluteIndexedAddressing(EOR, this.regs.Y); break;
                case 0x79: AbsoluteIndexedAddressing(ADC, this.regs.Y); break;
                case 0x99: AbsoluteIndexedAddressing(STA, this.regs.Y); break;
                case 0xb9: AbsoluteIndexedAddressing(LDA, this.regs.Y); break;
                case 0xd9: AbsoluteIndexedAddressing(CMP, this.regs.Y); break;
                case 0xf9: AbsoluteIndexedAddressing(SBC, this.regs.Y); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1a  NOP*    NOP*    NOP*    NOP*    TXS     TSX     NOP*    NOP*    Implied
                case 0x1a: ImpliedAddressing(NOP); break;
                case 0x3a: ImpliedAddressing(NOP); break;
                case 0x5a: ImpliedAddressing(NOP); break;
                case 0x7a: ImpliedAddressing(NOP); break;
                case 0x9a: ImpliedAddressing(TXS); break;
                case 0xba: ImpliedAddressing(TSX); break;
                case 0xda: ImpliedAddressing(NOP); break;
                case 0xfa: ImpliedAddressing(NOP); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                case 0x1b: AbsoluteIndexedAddressing(SLO, this.regs.Y); break;
                case 0x3b: AbsoluteIndexedAddressing(RLA, this.regs.Y); break;
                case 0x5b: AbsoluteIndexedAddressing(SRE, this.regs.Y); break;
                case 0x7b: AbsoluteIndexedAddressing(RRA, this.regs.Y); break;
                case 0xdb: AbsoluteIndexedAddressing(DCP, this.regs.Y); break;
                case 0xfb: AbsoluteIndexedAddressing(ISC, this.regs.Y); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                case 0x1c: AbsoluteIndexedAddressing(NOP, this.regs.X); break;
                case 0x3c: AbsoluteIndexedAddressing(NOP, this.regs.X); break;
                case 0x5c: AbsoluteIndexedAddressing(NOP, this.regs.X); break;
                case 0x7c: AbsoluteIndexedAddressing(NOP, this.regs.X); break;
                case 0xbc: AbsoluteIndexedAddressing(LDY, this.regs.X); break;
                case 0xdc: AbsoluteIndexedAddressing(NOP, this.regs.X); break;
                case 0xfc: AbsoluteIndexedAddressing(NOP, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                case 0x1d: AbsoluteIndexedAddressing(ORA, this.regs.X); break;
                case 0x3d: AbsoluteIndexedAddressing(AND, this.regs.X); break;
                case 0x5d: AbsoluteIndexedAddressing(EOR, this.regs.X); break;
                case 0x7d: AbsoluteIndexedAddressing(ADC, this.regs.X); break;
                case 0x9d: AbsoluteIndexedAddressing(STA, this.regs.X); break;
                case 0xbd: AbsoluteIndexedAddressing(LDA, this.regs.X); break;
                case 0xdd: AbsoluteIndexedAddressing(CMP, this.regs.X); break;
                case 0xfd: AbsoluteIndexedAddressing(SBC, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                case 0x1e: AbsoluteIndexedAddressing(ASL, this.regs.X); break;
                case 0x3e: AbsoluteIndexedAddressing(ROL, this.regs.X); break;
                case 0x5e: AbsoluteIndexedAddressing(LSR, this.regs.X); break;
                case 0x7e: AbsoluteIndexedAddressing(ROR, this.regs.X); break;
                case 0xbe: AbsoluteIndexedAddressing(LDX, this.regs.Y); break;
                case 0xde: AbsoluteIndexedAddressing(DEC, this.regs.X); break;
                case 0xfe: AbsoluteIndexedAddressing(INC, this.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x
                case 0x1f: AbsoluteIndexedAddressing(SLO, this.regs.X); break;
                case 0x3f: AbsoluteIndexedAddressing(RLA, this.regs.X); break;
                case 0x5f: AbsoluteIndexedAddressing(SRE, this.regs.X); break;
                case 0x7f: AbsoluteIndexedAddressing(RRA, this.regs.X); break;
                case 0xbf: AbsoluteIndexedAddressing(LAX, this.regs.Y); break;
                case 0xdf: AbsoluteIndexedAddressing(DCP, this.regs.X); break;
                case 0xff: AbsoluteIndexedAddressing(ISC, this.regs.X); break;

                default: throw new NotImplementedException($"Missing Opcode: {this.currentOpcode:X2} @ {this.CycleCount}");
            };
        }
    }
}
