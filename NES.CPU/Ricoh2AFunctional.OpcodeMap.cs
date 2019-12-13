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

            cpu.currentOpcodeAddress.Ptr = cpu.regs.PC.Ptr;
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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                case 0x03: IndexedIndirectAddressing(cpu, SLO); break;
                case 0x23: IndexedIndirectAddressing(cpu, RLA); break;
                case 0x43: IndexedIndirectAddressing(cpu, SRE); break;
                case 0x63: IndexedIndirectAddressing(cpu, RRA); break;
                case 0x83: IndexedIndirectAddressing(cpu, SAX); break;
                case 0xa3: IndexedIndirectAddressing(cpu, LAX); break;
                case 0xc3: IndexedIndirectAddressing(cpu, DCP); break;
                case 0xe3: IndexedIndirectAddressing(cpu, ISC); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage
                case 0x07: ZeroPageAddressing(cpu, SLO); break;
                case 0x27: ZeroPageAddressing(cpu, RLA); break;
                case 0x47: ZeroPageAddressing(cpu, SRE); break;
                case 0x67: ZeroPageAddressing(cpu, RRA); break;
                case 0x87: ZeroPageAddressing(cpu, SAX); break;
                case 0xa7: ZeroPageAddressing(cpu, LAX); break;
                case 0xc7: ZeroPageAddressing(cpu, DCP); break;
                case 0xe7: ZeroPageAddressing(cpu, ISC); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0b  ANC**   ANC**   ASR**   ARR**   ANE**   LXA**   SBX**   SBC*    Immediate
                case 0xeb: ImmediateAddressing(cpu, SBC); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute
                case 0x0f: AbsoluteAddressing(cpu, SLO); break;
                case 0x2f: AbsoluteAddressing(cpu, RLA); break;
                case 0x4f: AbsoluteAddressing(cpu, SRE); break;
                case 0x6f: AbsoluteAddressing(cpu, RRA); break;
                case 0x8f: AbsoluteAddressing(cpu, SAX); break;
                case 0xaf: AbsoluteAddressing(cpu, LAX); break;
                case 0xcf: AbsoluteAddressing(cpu, DCP); break;
                case 0xef: AbsoluteAddressing(cpu, ISC); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                case 0x11: IndirectIndexedAddressing(cpu, ORA); break;
                case 0x31: IndirectIndexedAddressing(cpu, AND); break;
                case 0x51: IndirectIndexedAddressing(cpu, EOR); break;
                case 0x71: IndirectIndexedAddressing(cpu, ADC); break;
                case 0x91: IndirectIndexedAddressing(cpu, STA); break;
                case 0xb1: IndirectIndexedAddressing(cpu, LDA); break;
                case 0xd1: IndirectIndexedAddressing(cpu, CMP); break;
                case 0xf1: IndirectIndexedAddressing(cpu, SBC); break;

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
                case 0x13: IndirectIndexedAddressing(cpu, SLO); break;
                case 0x33: IndirectIndexedAddressing(cpu, RLA); break;
                case 0x53: IndirectIndexedAddressing(cpu, SRE); break;
                case 0x73: IndirectIndexedAddressing(cpu, RRA); break;
                case 0xb3: IndirectIndexedAddressing(cpu, LAX); break;
                case 0xd3: IndirectIndexedAddressing(cpu, DCP); break;
                case 0xf3: IndirectIndexedAddressing(cpu, ISC); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                case 0x14: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x34: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x54: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x74: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x94: ZeroPageIndexedAddressing(cpu, STY, cpu.regs.X); break;
                case 0xb4: ZeroPageIndexedAddressing(cpu, LDY, cpu.regs.X); break;
                case 0xd4: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0xf4: ZeroPageIndexedAddressing(cpu, NOP, cpu.regs.X); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                case 0x16: ZeroPageIndexedAddressing(cpu, ASL, cpu.regs.X); break;
                case 0x36: ZeroPageIndexedAddressing(cpu, ROL, cpu.regs.X); break;
                case 0x56: ZeroPageIndexedAddressing(cpu, LSR, cpu.regs.X); break;
                case 0x76: ZeroPageIndexedAddressing(cpu, ROR, cpu.regs.X); break;
                case 0x96: ZeroPageIndexedAddressing(cpu, STX, cpu.regs.Y); break;
                case 0xb6: ZeroPageIndexedAddressing(cpu, LDX, cpu.regs.Y); break;
                case 0xd6: ZeroPageIndexedAddressing(cpu, DEC, cpu.regs.X); break;
                case 0xf6: ZeroPageIndexedAddressing(cpu, INC, cpu.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x
                case 0x17: ZeroPageIndexedAddressing(cpu, SLO, cpu.regs.X); break;
                case 0x37: ZeroPageIndexedAddressing(cpu, RLA, cpu.regs.X); break;
                case 0x57: ZeroPageIndexedAddressing(cpu, SRE, cpu.regs.X); break;
                case 0x77: ZeroPageIndexedAddressing(cpu, RRA, cpu.regs.X); break;
                case 0x97: ZeroPageIndexedAddressing(cpu, SAX, cpu.regs.Y); break;
                case 0xb7: ZeroPageIndexedAddressing(cpu, LAX, cpu.regs.Y); break;
                case 0xd7: ZeroPageIndexedAddressing(cpu, DCP, cpu.regs.X); break;
                case 0xf7: ZeroPageIndexedAddressing(cpu, ISC, cpu.regs.X); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                case 0x19: AbsoluteIndexedAddressing(cpu, ORA, cpu.regs.Y); break;
                case 0x39: AbsoluteIndexedAddressing(cpu, AND, cpu.regs.Y); break;
                case 0x59: AbsoluteIndexedAddressing(cpu, EOR, cpu.regs.Y); break;
                case 0x79: AbsoluteIndexedAddressing(cpu, ADC, cpu.regs.Y); break;
                case 0x99: AbsoluteIndexedAddressing(cpu, STA, cpu.regs.Y); break;
                case 0xb9: AbsoluteIndexedAddressing(cpu, LDA, cpu.regs.Y); break;
                case 0xd9: AbsoluteIndexedAddressing(cpu, CMP, cpu.regs.Y); break;
                case 0xf9: AbsoluteIndexedAddressing(cpu, SBC, cpu.regs.Y); break;

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

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                case 0x1b: AbsoluteIndexedAddressing(cpu, SLO, cpu.regs.Y); break;
                case 0x3b: AbsoluteIndexedAddressing(cpu, RLA, cpu.regs.Y); break;
                case 0x5b: AbsoluteIndexedAddressing(cpu, SRE, cpu.regs.Y); break;
                case 0x7b: AbsoluteIndexedAddressing(cpu, RRA, cpu.regs.Y); break;
                case 0xdb: AbsoluteIndexedAddressing(cpu, DCP, cpu.regs.Y); break;
                case 0xfb: AbsoluteIndexedAddressing(cpu, ISC, cpu.regs.Y); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                case 0x1c: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x3c: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x5c: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0x7c: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0xbc: AbsoluteIndexedAddressing(cpu, LDY, cpu.regs.X); break;
                case 0xdc: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;
                case 0xfc: AbsoluteIndexedAddressing(cpu, NOP, cpu.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                case 0x1d: AbsoluteIndexedAddressing(cpu, ORA, cpu.regs.X); break;
                case 0x3d: AbsoluteIndexedAddressing(cpu, AND, cpu.regs.X); break;
                case 0x5d: AbsoluteIndexedAddressing(cpu, EOR, cpu.regs.X); break;
                case 0x7d: AbsoluteIndexedAddressing(cpu, ADC, cpu.regs.X); break;
                case 0x9d: AbsoluteIndexedAddressing(cpu, STA, cpu.regs.X); break;
                case 0xbd: AbsoluteIndexedAddressing(cpu, LDA, cpu.regs.X); break;
                case 0xdd: AbsoluteIndexedAddressing(cpu, CMP, cpu.regs.X); break;
                case 0xfd: AbsoluteIndexedAddressing(cpu, SBC, cpu.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                case 0x1e: AbsoluteIndexedAddressing(cpu, ASL, cpu.regs.X); break;
                case 0x3e: AbsoluteIndexedAddressing(cpu, ROL, cpu.regs.X); break;
                case 0x5e: AbsoluteIndexedAddressing(cpu, LSR, cpu.regs.X); break;
                case 0x7e: AbsoluteIndexedAddressing(cpu, ROR, cpu.regs.X); break;
                case 0xbe: AbsoluteIndexedAddressing(cpu, LDX, cpu.regs.Y); break;
                case 0xde: AbsoluteIndexedAddressing(cpu, DEC, cpu.regs.X); break;
                case 0xfe: AbsoluteIndexedAddressing(cpu, INC, cpu.regs.X); break;

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x
                case 0x1f: AbsoluteIndexedAddressing(cpu, SLO, cpu.regs.X); break;
                case 0x3f: AbsoluteIndexedAddressing(cpu, RLA, cpu.regs.X); break;
                case 0x5f: AbsoluteIndexedAddressing(cpu, SRE, cpu.regs.X); break;
                case 0x7f: AbsoluteIndexedAddressing(cpu, RRA, cpu.regs.X); break;
                case 0xbf: AbsoluteIndexedAddressing(cpu, LAX, cpu.regs.Y); break;
                case 0xdf: AbsoluteIndexedAddressing(cpu, DCP, cpu.regs.X); break;
                case 0xff: AbsoluteIndexedAddressing(cpu, ISC, cpu.regs.X); break;

                default: throw new NotImplementedException($"Missing Opcode: {cpu.currentOpcode:X2} @ {cpu.CycleCount}");
            };
        }
    }
}
