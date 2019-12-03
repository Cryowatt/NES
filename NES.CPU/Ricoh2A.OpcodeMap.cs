using System;
using System.Collections.Generic;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        private IEnumerable<object> GetMicrocode(byte opcode)
        {
            IEnumerable<object> instructionCycles = opcode switch
            {
                //set  00      20      40      60      80      a0      c0      e0      mode
                //+00  BRK     JSR     RTI     RTS     NOP*    LDY     CPY     CPX     Impl/immed
                0x00 => BRK(),
                0x20 => JSR(),
                0x40 => RTI(),
                0x60 => RTS(),
                //0x80 => StubAddressing(NOP),
                0xa0 => ImmediateAddressing(LDY),
                0xc0 => ImmediateAddressing(CPY),
                0xe0 => ImmediateAddressing(CPX),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+01  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir,x)
                0x01 => IndexedIndirectAddressing(ORA),
                0x21 => IndexedIndirectAddressing(AND),
                0x41 => IndexedIndirectAddressing(EOR),
                0x61 => IndexedIndirectAddressing(ADC),
                0x81 => IndexedIndirectAddressing(STA),
                0xa1 => IndexedIndirectAddressing(LDA),
                0xc1 => IndexedIndirectAddressing(CMP),
                0xe1 => IndexedIndirectAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+02   t       t       t       t      NOP*t   LDX     NOP*t   NOP*t     ? /immed
                0x02 => StubAddressing(STP),
                0x22 => StubAddressing(STP),
                0x42 => StubAddressing(STP),
                0x62 => StubAddressing(STP),
                0x82 => StubAddressing(NOP),
                0xa2 => ImmediateAddressing(LDX),
                0xc2 => StubAddressing(NOP),
                0xe2 => StubAddressing(NOP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                //0x03 => StubAddressing(SLO),
                //0x23 => StubAddressing(RLA),
                //0x43 => StubAddressing(SRE),
                //0x63 => StubAddressing(RRA),
                //0x83 => StubAddressing(SAX),
                //0xa3 => StubAddressing(LAX),
                //0xc3 => StubAddressing(DCP),
                //0xe3 => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+04  NOP*    BIT     NOP*    NOP*    STY     LDY     CPY     CPX     Zeropage
                //0x04 => StubAddressing(NOP),
                0x24 => ZeroPageAddressing(BIT),
                //0x44 => StubAddressing(NOP),
                //0x64 => StubAddressing(NOP),
                0x84 => ZeroPageAddressing(STY),
                0xa4 => ZeroPageAddressing(LDY),
                0xc4 => ZeroPageAddressing(CPY),
                0xe4 => ZeroPageAddressing(CPX),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+05  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage
                0x05 => ZeroPageAddressing(ORA),
                0x25 => ZeroPageAddressing(AND),
                0x45 => ZeroPageAddressing(EOR),
                0x65 => ZeroPageAddressing(ADC),
                0x85 => ZeroPageAddressing(STA),
                0xa5 => ZeroPageAddressing(LDA),
                0xc5 => ZeroPageAddressing(CMP),
                0xe5 => ZeroPageAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+06  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Zeropage
                0x06 => ZeroPageAddressing(ASL),
                0x26 => ZeroPageAddressing(ROL),
                0x46 => ZeroPageAddressing(LSR),
                0x66 => ZeroPageAddressing(ROR),
                0x86 => ZeroPageAddressing(STX),
                0xa6 => ZeroPageAddressing(LDX),
                0xc6 => ZeroPageAddressing(DEC),
                0xe6 => ZeroPageAddressing(INC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage
                //0x07 => StubAddressing(SLO),
                //0x27 => StubAddressing(RLA),
                //0x47 => StubAddressing(SRE),
                //0x67 => StubAddressing(RRA),
                //0x87 => StubAddressing(SAX),
                //0xa7 => StubAddressing(LAX),
                //0xc7 => StubAddressing(DCP),
                //0xe7 => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+08  PHP     PLP     PHA     PLA     DEY     TAY     INY     INX     Implied
                0x08 => PHP(),
                0x28 => PLP(),
                0x48 => PHA(),
                0x68 => PLA(),
                0x88 => ImpliedAddressing(DEY),
                0xa8 => ImpliedAddressing(TAY),
                0xc8 => ImpliedAddressing(INY),
                0xe8 => ImpliedAddressing(INX),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+09  ORA     AND     EOR     ADC     NOP*    LDA     CMP     SBC     Immediate
                0x09 => ImmediateAddressing(ORA),
                0x29 => ImmediateAddressing(AND),
                0x49 => ImmediateAddressing(EOR),
                0x69 => ImmediateAddressing(ADC),
                //0x89 => StubAddressing(NOP),
                0xa9 => ImmediateAddressing(LDA),
                0xc9 => ImmediateAddressing(CMP),
                0xe9 => ImmediateAddressing(SBC),

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
                //0x0b => StubAddressing(ANC),
                //0x2b => StubAddressing(ANC),
                //0x4b => StubAddressing(ALR),
                //0x6b => StubAddressing(ARR),
                //0x8b => StubAddressing(XAA),
                //0xab => StubAddressing(LAX),
                //0xcb => StubAddressing(AXS),
                //0xeb => StubAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0c  NOP*    BIT     JMP     JMP ()  STY     LDY     CPY     CPX     Absolute
                //0x0c => StubAddressing(NOP),
                0x2c => AbsoluteAddressing(BIT),
                0x4c => AbsoluteAddressing((Action<Address>)JMP),
                0x6c => AbsoluteIndirectAddressing(JMP),
                0x8c => AbsoluteAddressing(STY),
                0xac => AbsoluteAddressing(LDY),
                0xcc => AbsoluteAddressing(CPY),
                0xec => AbsoluteAddressing(CPX),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute
                0x0d => AbsoluteAddressing(ORA),
                0x2d => AbsoluteAddressing(AND),
                0x4d => AbsoluteAddressing(EOR),
                0x6d => AbsoluteAddressing(ADC),
                0x8d => AbsoluteAddressing(STA),
                0xad => AbsoluteAddressing(LDA),
                0xcd => AbsoluteAddressing(CMP),
                0xed => AbsoluteAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0e  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Absolute
                0x0e => AbsoluteAddressing(ASL),
                0x2e => AbsoluteAddressing(ROL),
                0x4e => AbsoluteAddressing(LSR),
                0x6e => AbsoluteAddressing(ROR),
                0x8e => AbsoluteAddressing(STX),
                0xae => AbsoluteAddressing(LDX),
                0xce => AbsoluteAddressing(DEC),
                0xee => AbsoluteAddressing(INC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute
                //0x0f => StubAddressing(SLO),
                //0x2f => StubAddressing(RLA),
                //0x4f => StubAddressing(SRE),
                //0x6f => StubAddressing(RRA),
                //0x8f => StubAddressing(SAX),
                //0xaf => StubAddressing(LAX),
                //0xcf => StubAddressing(DCP),
                //0xef => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+10  BPL     BMI     BVC     BVS     BCC     BCS     BNE     BEQ     Relative
                0x10 => RelativeAddressing(BPL),
                0x30 => RelativeAddressing(BMI),
                0x50 => RelativeAddressing(BVC),
                0x70 => RelativeAddressing(BVS),
                0x90 => RelativeAddressing(BCC),
                0xb0 => RelativeAddressing(BCS),
                0xd0 => RelativeAddressing(BNE),
                0xf0 => RelativeAddressing(BEQ),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                0x11 => IndirectIndexedAddressing(ORA),
                0x31 => IndirectIndexedAddressing(AND),
                0x51 => IndirectIndexedAddressing(EOR),
                0x71 => IndirectIndexedAddressing(ADC),
                0x91 => IndirectIndexedAddressing(STA),
                0xb1 => IndirectIndexedAddressing(LDA),
                0xd1 => IndirectIndexedAddressing(CMP),
                0xf1 => IndirectIndexedAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+12   t       t       t       t       t       t       t       t         ?
                //0x12 => StubAddressing(STP),
                //0x32 => StubAddressing(STP),
                //0x52 => StubAddressing(STP),
                //0x72 => StubAddressing(STP),
                //0x92 => StubAddressing(STP),
                //0xb2 => StubAddressing(STP),
                //0xd2 => StubAddressing(STP),
                //0xf2 => StubAddressing(STP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+13  SLO*    RLA*    SRE*    RRA*    SHA**   LAX*    DCP*    ISB*    (indir),y
                //0x13 => StubAddressing(SLO),
                //0x33 => StubAddressing(RLA),
                //0x53 => StubAddressing(SRE),
                //0x73 => StubAddressing(RRA),
                //0x93 => StubAddressing(AHX),
                //0xb3 => StubAddressing(LAX),
                //0xd3 => StubAddressing(DCP),
                //0xf3 => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                //0x14 => StubAddressing(NOP),
                //0x34 => StubAddressing(NOP),
                //0x54 => StubAddressing(NOP),
                //0x74 => StubAddressing(NOP),
                0x94 => ZeroPageIndexedAddressing(STY, this.regs.X),
                0xb4 => ZeroPageIndexedAddressing(LDY, this.regs.X),
                //0xd4 => StubAddressing(NOP),
                //0xf4 => StubAddressing(NOP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+15  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage,x
                0x15 => StubAddressing(ORA),
                0x35 => StubAddressing(AND),
                0x55 => ZeroPageIndexedAddressing(EOR, this.regs.X),
                0x75 => ZeroPageIndexedAddressing(ADC, this.regs.X),
                0x95 => ZeroPageIndexedAddressing(STA, this.regs.X),
                0xb5 => StubAddressing(LDA),
                0xd5 => StubAddressing(CMP),
                0xf5 => StubAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                0x16 => ZeroPageIndexedAddressing(ASL, this.regs.X),
                0x36 => ZeroPageIndexedAddressing(ROL, this.regs.X),
                0x56 => ZeroPageIndexedAddressing(LSR, this.regs.X),
                0x76 => ZeroPageIndexedAddressing(ROR, this.regs.X),
                0x96 => ZeroPageIndexedAddressing(STX, this.regs.X),
                0xb6 => ZeroPageIndexedAddressing(LDX, this.regs.X),
                0xd6 => ZeroPageIndexedAddressing(DEC, this.regs.X),
                0xf6 => ZeroPageIndexedAddressing(INC, this.regs.X),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x
                //0x17 => StubAddressing(SLO),
                //0x37 => StubAddressing(RLA),
                //0x57 => StubAddressing(SRE),
                //0x77 => StubAddressing(RRA),
                //0x97 => StubAddressing(SAX),
                //0xb7 => StubAddressing(LAX),
                //0xd7 => StubAddressing(DCP),
                //0xf7 => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+18  CLC     SEC     CLI     SEI     TYA     CLV     CLD     SED     Implied
                0x18 => ImpliedAddressing(CLC),
                0x38 => ImpliedAddressing(SEC),
                0x58 => ImpliedAddressing(CLI),
                0x78 => ImpliedAddressing(SEI),
                0x98 => ImpliedAddressing(TYA),
                0xb8 => ImpliedAddressing(CLV),
                0xd8 => ImpliedAddressing(CLD),
                0xf8 => ImpliedAddressing(SED),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                0x19 => StubAddressing(ORA),
                0x39 => StubAddressing(AND),
                0x59 => AbsoluteIndexedAddressing(EOR, this.regs.Y),
                0x79 => AbsoluteIndexedAddressing(ADC, this.regs.Y),
                0x99 => AbsoluteIndexedAddressing(STA, this.regs.Y),
                0xb9 => StubAddressing(LDA),
                0xd9 => StubAddressing(CMP),
                0xf9 => StubAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1a  NOP*    NOP*    NOP*    NOP*    TXS     TSX     NOP*    NOP*    Implied
                //0x1a => StubAddressing(NOP),
                //0x3a => StubAddressing(NOP),
                //0x5a => StubAddressing(NOP),
                //0x7a => StubAddressing(NOP),
                0x9a => ImpliedAddressing(TXS),
                0xba => ImpliedAddressing(TSX),
                //0xda => StubAddressing(NOP),
                //0xfa => StubAddressing(NOP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                //0x1b => StubAddressing(SLO),
                //0x3b => StubAddressing(RLA),
                //0x5b => StubAddressing(SRE),
                //0x7b => StubAddressing(RRA),
                //0x9b => StubAddressing(TAS),
                //0xbb => StubAddressing(LAS),
                //0xdb => StubAddressing(DCP),
                //0xfb => StubAddressing(ISC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                //0x1c => StubAddressing(NOP),
                //0x3c => StubAddressing(NOP),
                //0x5c => StubAddressing(NOP),
                //0x7c => StubAddressing(NOP),
                //0x9c => StubAddressing(SHY),
                0xbc => StubAddressing(LDY),
                //0xdc => StubAddressing(NOP),
                //0xfc => StubAddressing(NOP),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                0x1d => AbsoluteIndexedAddressing(ORA, this.regs.X),
                0x3d => AbsoluteIndexedAddressing(AND, this.regs.X),
                0x5d => AbsoluteIndexedAddressing(EOR, this.regs.X),
                0x7d => AbsoluteIndexedAddressing(ADC, this.regs.X),
                0x9d => AbsoluteIndexedAddressing(STA, this.regs.X),
                0xbd => AbsoluteIndexedAddressing(LDA, this.regs.X),
                0xdd => AbsoluteIndexedAddressing(CMP, this.regs.X),
                0xfd => StubAddressing(SBC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                0x1e => StubAddressing(ASL),
                0x3e => StubAddressing(ROL),
                0x5e => StubAddressing(LSR),
                0x7e => StubAddressing(ROR),
                //0x9e => StubAddressing(SHX),
                0xbe => StubAddressing(LDX),
                0xde => StubAddressing(DEC),
                0xfe => StubAddressing(INC),

                //set  00      20      40      60      80      a0      c0      e0      mode
                //+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x
                //0x1f => StubAddressing(SLO),
                //0x3f => StubAddressing(RLA),
                //0x5f => StubAddressing(SRE),
                //0x7f => StubAddressing(RRA),
                //0x9f => StubAddressing(AHX),
                //0xbf => StubAddressing(LAX),
                //0xdf => StubAddressing(DCP),
                //0xff => StubAddressing(ISC),

                _ => throw new NotImplementedException($"Missing Opcode: {opcode:X2}"),
            };
            return instructionCycles;
        }
    }
}
