using System;
using System.Collections.Generic;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional
    {
        private static void QueueOpCode(Ricoh2AFunctional cpu)
        {
            if (cpu.InstructionTrace != null && cpu.lastInstruction != null)
            {
                cpu.InstructionTrace(cpu.lastInstruction);
            }

            cpu.currentOpcodeAddress = cpu.regs.PC;
            cpu.currentOpcode = cpu.Read(cpu.regs.PC.Ptr++);

            switch (cpu.currentOpcode)
            {
                //set  00      20      40      60      80      a0      c0      e0      mode
                //+00  BRK     JSR     RTI     RTS     NOP*    LDY     CPY     CPX     Impl/immed
                case 0x00: BRK(cpu); break;
                case 0x20: JSR(cpu); break;
                case 0x40: RTI(cpu); break;
                case 0x60: RTS(cpu); break;
                case 0x80: ImmediateAddressing(cpu, NOP); break;
                case 0xa0: ImmediateAddressing(cpu, LDY); break;
                case 0xc0: ImmediateAddressing(cpu, CPY); break;
                case 0xe0: ImmediateAddressing(cpu, CPX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+01  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir,x)
                case 0x01: IndexedIndirectAddressing(cpu, Operation(ORA)); break;
                case 0x21: IndexedIndirectAddressing(cpu, Operation(AND)); break;
                case 0x41: IndexedIndirectAddressing(cpu, Operation(EOR)); break;
                case 0x61: IndexedIndirectAddressing(cpu, Operation(ADC)); break;
                case 0x81: IndexedIndirectAddressing(cpu, Operation(STA)); break;
                case 0xa1: IndexedIndirectAddressing(cpu, Operation(LDA)); break;
                case 0xc1: IndexedIndirectAddressing(cpu, Operation(CMP)); break;
                case 0xe1: IndexedIndirectAddressing(cpu, Operation(SBC)); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+02   t       t       t       t      NOP*t   LDX     NOP*t   NOP*t     ? /immed
                case 0xa2: ImmediateAddressing(cpu, LDX); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                //0x03 => IndexedIndirectAddressing(SLO),
                //0x23 => IndexedIndirectAddressing(RLA),
                //0x43 => IndexedIndirectAddressing(SRE),
                //0x63 => IndexedIndirectAddressing(RRA),
                //0x83 => IndexedIndirectAddressing(SAX),
                //0xa3 => IndexedIndirectAddressing(LAX),
                //0xc3 => IndexedIndirectAddressing(DCP),
                //0xe3 => IndexedIndirectAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+04  NOP*    BIT     NOP*    NOP*    STY     LDY     CPY     CPX     Zeropage
                case 0x04: ZeroPageAddressing(cpu, Operation(NOP)); break;
                case 0x24: ZeroPageAddressing(cpu, Operation(BIT)); break;
                case 0x44: ZeroPageAddressing(cpu, Operation(NOP)); break;
                case 0x64: ZeroPageAddressing(cpu, Operation(NOP)); break;
                case 0x84: ZeroPageAddressing(cpu, Operation(STY)); break;
                case 0xa4: ZeroPageAddressing(cpu, Operation(LDY)); break;
                case 0xc4: ZeroPageAddressing(cpu, Operation(CPY)); break;
                case 0xe4: ZeroPageAddressing(cpu, Operation(CPX)); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+05  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage
                case 0x05: ZeroPageAddressing(cpu, Operation(ORA)); break;
                case 0x25: ZeroPageAddressing(cpu, Operation(AND)); break;
                case 0x45: ZeroPageAddressing(cpu, Operation(EOR)); break;
                case 0x65: ZeroPageAddressing(cpu, Operation(ADC)); break;
                case 0x85: ZeroPageAddressing(cpu, Operation(STA)); break;
                case 0xa5: ZeroPageAddressing(cpu, Operation(LDA)); break;
                case 0xc5: ZeroPageAddressing(cpu, Operation(CMP)); break;
                case 0xe5: ZeroPageAddressing(cpu, Operation(SBC)); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+06  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Zeropage
                case 0x06: ZeroPageAddressing(cpu, Operation(ASL)); break;
                case 0x26: ZeroPageAddressing(cpu, Operation(ROL)); break;
                case 0x46: ZeroPageAddressing(cpu, Operation(LSR)); break;
                case 0x66: ZeroPageAddressing(cpu, Operation(ROR)); break;
                case 0x86: ZeroPageAddressing(cpu, Operation(STX)); break;
                case 0xa6: ZeroPageAddressing(cpu, Operation(LDX)); break;
                case 0xc6: ZeroPageAddressing(cpu, Operation(DEC)); break;
                case 0xe6: ZeroPageAddressing(cpu, Operation(INC)); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage
                //0x07 => ZeroPageAddressing(SLO),
                //0x27 => ZeroPageAddressing(RLA),
                //0x47 => ZeroPageAddressing(SRE),
                //0x67 => ZeroPageAddressing(RRA),
                //0x87 => ZeroPageAddressing(SAX),
                //0xa7 => ZeroPageAddressing(LAX),
                //0xc7 => ZeroPageAddressing(DCP),
                //0xe7 => ZeroPageAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+08  PHP     PLP     PHA     PLA     DEY     TAY     INY     INX     Implied
                case 0x08: PHP(cpu); break;
                case 0x28: PLP(cpu); break;
                case 0x48: PHA(cpu); break;
                case 0x68: PLA(cpu); break;
                case 0x88: ImpliedAddressing(cpu, DEY); break;
                case 0xa8: ImpliedAddressing(cpu, TAY); break;
                case 0xc8: ImpliedAddressing(cpu, INY); break;
                case 0xe8: ImpliedAddressing(cpu, INX); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+09  ORA     AND     EOR     ADC     NOP*    LDA     CMP     SBC     Immediate
                case 0x09: ImmediateAddressing(cpu, ORA); break;
                case 0x29: ImmediateAddressing(cpu, AND); break;
                case 0x49: ImmediateAddressing(cpu, EOR); break;
                case 0x69: ImmediateAddressing(cpu, ADC); break;
                case 0xa9: ImmediateAddressing(cpu, LDA); break;
                case 0xc9: ImmediateAddressing(cpu, CMP); break;
                case 0xe9: ImmediateAddressing(cpu, SBC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0a  ASL     ROL     LSR     ROR     TXA     TAX     DEX     NOP     Accu/imp
                case 0x0a: AccumulatorAddressing(cpu, ASL); break;
                case 0x2a: AccumulatorAddressing(cpu, ROL); break;
                case 0x4a: AccumulatorAddressing(cpu, LSR); break;
                case 0x6a: AccumulatorAddressing(cpu, ROR); break;
                case 0x8a: ImpliedAddressing(cpu, TXA); break;
                case 0xaa: ImpliedAddressing(cpu, TAX); break;
                case 0xca: ImpliedAddressing(cpu, DEX); break;
                case 0xea: ImpliedAddressing(cpu, NOP); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+0b  ANC**   ANC**   ASR**   ARR**   ANE**   LXA**   SBX**   SBC*    Immediate
                ////0x0b => StubAddressing(ANC),
                ////0x2b => StubAddressing(ANC),
                ////0x4b => StubAddressing(ALR),
                ////0x6b => StubAddressing(ARR),
                ////0x8b => StubAddressing(XAA),
                ////0xab => StubAddressing(LAX),
                ////0xcb => StubAddressing(AXS),
                //0xeb => ImmediateAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0c  NOP*    BIT     JMP     JMP ()  STY     LDY     CPY     CPX     Absolute
                case 0x0c: AbsoluteAddressing(cpu, Operation(NOP)); break;
                case 0x2c: AbsoluteAddressing(cpu, Operation(BIT)); break;
                case 0x4c: AbsoluteAddressing(cpu, JMP); break;
                case 0x6c: AbsoluteIndirectAddressing(cpu, JMP); break;
                case 0x8c: AbsoluteAddressing(cpu, Operation(STY)); break;
                case 0xac: AbsoluteAddressing(cpu, Operation(LDY)); break;
                case 0xcc: AbsoluteAddressing(cpu, Operation(CPY)); break;
                case 0xec: AbsoluteAddressing(cpu, Operation(CPX)); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute
                case 0x0d: AbsoluteAddressing(cpu, Operation(ORA)); break;
                case 0x2d: AbsoluteAddressing(cpu, Operation(AND)); break;
                case 0x4d: AbsoluteAddressing(cpu, Operation(EOR)); break;
                case 0x6d: AbsoluteAddressing(cpu, Operation(ADC)); break;
                case 0x8d: AbsoluteAddressing(cpu, Operation(STA)); break;
                case 0xad: AbsoluteAddressing(cpu, Operation(LDA)); break;
                case 0xcd: AbsoluteAddressing(cpu, Operation(CMP)); break;
                case 0xed: AbsoluteAddressing(cpu, Operation(SBC)); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0e  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Absolute
                case 0x0e: AbsoluteAddressing(cpu, Operation(ASL)); break;
                case 0x2e: AbsoluteAddressing(cpu, Operation(ROL)); break;
                case 0x4e: AbsoluteAddressing(cpu, Operation(LSR)); break;
                case 0x6e: AbsoluteAddressing(cpu, Operation(ROR)); break;
                case 0x8e: AbsoluteAddressing(cpu, Operation(STX)); break;
                case 0xae: AbsoluteAddressing(cpu, Operation(LDX)); break;
                case 0xce: AbsoluteAddressing(cpu, Operation(DEC)); break;
                case 0xee: AbsoluteAddressing(cpu, Operation(INC)); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute
                //0x0f => AbsoluteAddressing(SLO),
                //0x2f => AbsoluteAddressing(RLA),
                //0x4f => AbsoluteAddressing(SRE),
                //0x6f => AbsoluteAddressing(RRA),
                //0x8f => AbsoluteAddressing(SAX),
                //0xaf => AbsoluteAddressing(LAX),
                //0xcf => AbsoluteAddressing(DCP),
                //0xef => AbsoluteAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+10  BPL     BMI     BVC     BVS     BCC     BCS     BNE     BEQ     Relative
                case 0x10: RelativeAddressing(cpu, BPL); break;
                case 0x30: RelativeAddressing(cpu, BMI); break;
                case 0x50: RelativeAddressing(cpu, BVC); break;
                case 0x70: RelativeAddressing(cpu, BVS); break;
                case 0x90: RelativeAddressing(cpu, BCC); break;
                case 0xb0: RelativeAddressing(cpu, BCS); break;
                case 0xd0: RelativeAddressing(cpu, BNE); break;
                case 0xf0: RelativeAddressing(cpu, BEQ); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                //0x11 => IndirectIndexedAddressing(ORA),
                //0x31 => IndirectIndexedAddressing(AND),
                //0x51 => IndirectIndexedAddressing(EOR),
                //0x71 => IndirectIndexedAddressing(ADC),
                //0x91 => IndirectIndexedAddressing(STA),
                //0xb1 => IndirectIndexedAddressing(LDA),
                //0xd1 => IndirectIndexedAddressing(CMP),
                //0xf1 => IndirectIndexedAddressing(SBC),

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

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+13  SLO*    RLA*    SRE*    RRA*    SHA**   LAX*    DCP*    ISB*    (indir),y
                //0x13 => IndirectIndexedAddressing(SLO),
                //0x33 => IndirectIndexedAddressing(RLA),
                //0x53 => IndirectIndexedAddressing(SRE),
                //0x73 => IndirectIndexedAddressing(RRA),
                ////0x93 => StubAddressing(AHX),
                //0xb3 => IndirectIndexedAddressing(LAX),
                //0xd3 => IndirectIndexedAddressing(DCP),
                //0xf3 => IndirectIndexedAddressing(ISC),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                //0x14 => ZeroPageIndexedAddressing(NOP, this.regs.X),
                //0x34 => ZeroPageIndexedAddressing(NOP, this.regs.X),
                //0x54 => ZeroPageIndexedAddressing(NOP, this.regs.X),
                //0x74 => ZeroPageIndexedAddressing(NOP, this.regs.X),
                //0x94 => ZeroPageIndexedAddressing(STY, this.regs.X),
                //0xb4 => ZeroPageIndexedAddressing(LDY, this.regs.X),
                //0xd4 => ZeroPageIndexedAddressing(NOP, this.regs.X),
                //0xf4 => ZeroPageIndexedAddressing(NOP, this.regs.X),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+15  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage,x
                case 0x15: ZeroPageIndexedAddressing(cpu, Operation(ORA), cpu.regs.X); break;
                case 0x35: ZeroPageIndexedAddressing(cpu, Operation(AND), cpu.regs.X); break;
                case 0x55: ZeroPageIndexedAddressing(cpu, Operation(EOR), cpu.regs.X); break;
                case 0x75: ZeroPageIndexedAddressing(cpu, Operation(ADC), cpu.regs.X); break;
                case 0x95: ZeroPageIndexedAddressing(cpu, Operation(STA), cpu.regs.X); break;
                case 0xb5: ZeroPageIndexedAddressing(cpu, Operation(LDA), cpu.regs.X); break;
                case 0xd5: ZeroPageIndexedAddressing(cpu, Operation(CMP), cpu.regs.X); break;
                case 0xf5: ZeroPageIndexedAddressing(cpu, Operation(SBC), cpu.regs.X); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                //0x16 => ZeroPageIndexedAddressing(ASL, this.regs.X),
                //0x36 => ZeroPageIndexedAddressing(ROL, this.regs.X),
                //0x56 => ZeroPageIndexedAddressing(LSR, this.regs.X),
                //0x76 => ZeroPageIndexedAddressing(ROR, this.regs.X),
                //0x96 => ZeroPageIndexedAddressing(STX, this.regs.Y),
                //0xb6 => ZeroPageIndexedAddressing(LDX, this.regs.Y),
                //0xd6 => ZeroPageIndexedAddressing(DEC, this.regs.X),
                //0xf6 => ZeroPageIndexedAddressing(INC, this.regs.X),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x
                //0x17 => ZeroPageIndexedAddressing(SLO, this.regs.X),
                //0x37 => ZeroPageIndexedAddressing(RLA, this.regs.X),
                //0x57 => ZeroPageIndexedAddressing(SRE, this.regs.X),
                //0x77 => ZeroPageIndexedAddressing(RRA, this.regs.X),
                //0x97 => ZeroPageIndexedAddressing(SAX, this.regs.Y),
                //0xb7 => ZeroPageIndexedAddressing(LAX, this.regs.Y),
                //0xd7 => ZeroPageIndexedAddressing(DCP, this.regs.X),
                //0xf7 => ZeroPageIndexedAddressing(ISC, this.regs.X),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+18  CLC     SEC     CLI     SEI     TYA     CLV     CLD     SED     Implied
                case 0x18: ImpliedAddressing(cpu, CLC); break;
                case 0x38: ImpliedAddressing(cpu, SEC); break;
                case 0x58: ImpliedAddressing(cpu, CLI); break;
                case 0x78: ImpliedAddressing(cpu, SEI); break;
                case 0x98: ImpliedAddressing(cpu, TYA); break;
                case 0xb8: ImpliedAddressing(cpu, CLV); break;
                case 0xd8: ImpliedAddressing(cpu, CLD); break;
                case 0xf8: ImpliedAddressing(cpu, SED); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                //0x19 => AbsoluteIndexedAddressing(ORA, this.regs.Y),
                //0x39 => AbsoluteIndexedAddressing(AND, this.regs.Y),
                //0x59 => AbsoluteIndexedAddressing(EOR, this.regs.Y),
                //0x79 => AbsoluteIndexedAddressing(ADC, this.regs.Y),
                //0x99 => AbsoluteIndexedAddressing(STA, this.regs.Y),
                //0xb9 => AbsoluteIndexedAddressing(LDA, this.regs.Y),
                //0xd9 => AbsoluteIndexedAddressing(CMP, this.regs.Y),
                //0xf9 => AbsoluteIndexedAddressing(SBC, this.regs.Y),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1a  NOP*    NOP*    NOP*    NOP*    TXS     TSX     NOP*    NOP*    Implied
                case 0x1a: ImpliedAddressing(cpu, NOP); break;
                case 0x3a: ImpliedAddressing(cpu, NOP); break;
                case 0x5a: ImpliedAddressing(cpu, NOP); break;
                case 0x7a: ImpliedAddressing(cpu, NOP); break;
                case 0x9a: ImpliedAddressing(cpu, TXS); break;
                case 0xba: ImpliedAddressing(cpu, TSX); break;
                case 0xda: ImpliedAddressing(cpu, NOP); break;
                case 0xfa: ImpliedAddressing(cpu, NOP); break;

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                //0x1b => AbsoluteIndexedAddressing(SLO, this.regs.Y),
                //0x3b => AbsoluteIndexedAddressing(RLA, this.regs.Y),
                //0x5b => AbsoluteIndexedAddressing(SRE, this.regs.Y),
                //0x7b => AbsoluteIndexedAddressing(RRA, this.regs.Y),
                ////0x9b => StubAddressing(TAS),
                ////0xbb => StubAddressing(LAS),
                //0xdb => AbsoluteIndexedAddressing(DCP, this.regs.Y),
                //0xfb => AbsoluteIndexedAddressing(ISC, this.regs.Y),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                //0x1c => AbsoluteIndexedAddressing(NOP, this.regs.X),
                //0x3c => AbsoluteIndexedAddressing(NOP, this.regs.X),
                //0x5c => AbsoluteIndexedAddressing(NOP, this.regs.X),
                //0x7c => AbsoluteIndexedAddressing(NOP, this.regs.X),
                ////0x9c => StubAddressing(SHY),
                //0xbc => AbsoluteIndexedAddressing(LDY, this.regs.X),
                //0xdc => AbsoluteIndexedAddressing(NOP, this.regs.X),
                //0xfc => AbsoluteIndexedAddressing(NOP, this.regs.X),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                //0x1d => AbsoluteIndexedAddressing(ORA, this.regs.X),
                //0x3d => AbsoluteIndexedAddressing(AND, this.regs.X),
                //0x5d => AbsoluteIndexedAddressing(EOR, this.regs.X),
                //0x7d => AbsoluteIndexedAddressing(ADC, this.regs.X),
                //0x9d => AbsoluteIndexedAddressing(STA, this.regs.X),
                //0xbd => AbsoluteIndexedAddressing(LDA, this.regs.X),
                //0xdd => AbsoluteIndexedAddressing(CMP, this.regs.X),
                //0xfd => AbsoluteIndexedAddressing(SBC, this.regs.X),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                //0x1e => AbsoluteIndexedAddressing(ASL, this.regs.X),
                //0x3e => AbsoluteIndexedAddressing(ROL, this.regs.X),
                //0x5e => AbsoluteIndexedAddressing(LSR, this.regs.X),
                //0x7e => AbsoluteIndexedAddressing(ROR, this.regs.X),
                ////0x9e => StubAddressing(SHX),
                //0xbe => AbsoluteIndexedAddressing(LDX, this.regs.Y),
                //0xde => AbsoluteIndexedAddressing(DEC, this.regs.X),
                //0xfe => AbsoluteIndexedAddressing(INC, this.regs.X),

                ////set  00      20      40      60      80      a0      c0      e0      mode
                ////+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x
                //0x1f => AbsoluteIndexedAddressing(SLO, this.regs.X),
                //0x3f => AbsoluteIndexedAddressing(RLA, this.regs.X),
                //0x5f => AbsoluteIndexedAddressing(SRE, this.regs.X),
                //0x7f => AbsoluteIndexedAddressing(RRA, this.regs.X),
                ////0x9f => StubAddressing(AHX),
                //0xbf => AbsoluteIndexedAddressing(LAX, this.regs.Y),
                //0xdf => AbsoluteIndexedAddressing(DCP, this.regs.X),
                //0xff => AbsoluteIndexedAddressing(ISC, this.regs.X),

                default: throw new NotImplementedException($"Missing Opcode: {cpu.currentOpcode:X2} @ {cpu.CycleCount}");
            };
        }
    }
}
