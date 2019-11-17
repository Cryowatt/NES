using System;
using System.Collections.Generic;
using System.Text;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        public IEnumerable<object> Process()
        {
            this.regs.PC.Low = Read(0xFFFC);
            yield return null;
            this.regs.PC.High = Read(0xFFFD);
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
                    0x80 => StubAddressing(NOP),
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
                    0x02 => StubAddressing(STP),
                    0x22 => StubAddressing(STP),
                    0x42 => StubAddressing(STP),
                    0x62 => StubAddressing(STP),
                    0x82 => StubAddressing(NOP),
                    0xa2 => StubAddressing(LDX),
                    0xc2 => StubAddressing(NOP),
                    0xe2 => StubAddressing(NOP),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+03  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    (indir,x)
                    0x03 => StubAddressing(SLO),
                    0x23 => StubAddressing(RLA),
                    0x43 => StubAddressing(SRE),
                    0x63 => StubAddressing(RRA),
                    0x83 => StubAddressing(SAX),
                    0xa3 => StubAddressing(LAX),
                    0xc3 => StubAddressing(DCP),
                    0xe3 => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+04  NOP*    BIT     NOP*    NOP*    STY     LDY     CPY     CPX     Zeropage
                    0x04 => StubAddressing(NOP),
                    0x24 => StubAddressing(BIT),
                    0x44 => StubAddressing(NOP),
                    0x64 => StubAddressing(NOP),
                    0x84 => StubAddressing(STY),
                    0xa4 => StubAddressing(LDY),
                    0xc4 => StubAddressing(CPY),
                    0xe4 => StubAddressing(CPX),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+05  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage
                    0x05 => StubAddressing(ORA),
                    0x25 => StubAddressing(AND),
                    0x45 => StubAddressing(EOR),
                    0x65 => StubAddressing(ADC),
                    0x85 => StubAddressing(STA),
                    0xa5 => StubAddressing(LDA),
                    0xc5 => StubAddressing(CMP),
                    0xe5 => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+06  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Zeropage
                    0x06 => StubAddressing(ASL),
                    0x26 => StubAddressing(ROL),
                    0x46 => StubAddressing(LSR),
                    0x66 => StubAddressing(ROR),
                    0x86 => StubAddressing(STX),
                    0xa6 => StubAddressing(LDX),
                    0xc6 => StubAddressing(DEC),
                    0xe6 => StubAddressing(INC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+07  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Zeropage
                    0x07 => StubAddressing(SLO),
                    0x27 => StubAddressing(RLA),
                    0x47 => StubAddressing(SRE),
                    0x67 => StubAddressing(RRA),
                    0x87 => StubAddressing(SAX),
                    0xa7 => StubAddressing(LAX),
                    0xc7 => StubAddressing(DCP),
                    0xe7 => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+08  PHP     PLP     PHA     PLA     DEY     TAY     INY     INX     Implied
                    0x08 => StubAddressing(PHP),
                    0x28 => StubAddressing(PLP),
                    0x48 => StubAddressing(PHA),
                    0x68 => StubAddressing(PLA),
                    0x88 => StubAddressing(DEY),
                    0xa8 => StubAddressing(TAY),
                    0xc8 => StubAddressing(INY),
                    0xe8 => StubAddressing(INX),

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
                    0x0b => StubAddressing(ANC),
                    0x2b => StubAddressing(ANC),
                    0x4b => StubAddressing(ASR),
                    0x6b => StubAddressing(ARR),
                    0x8b => StubAddressing(ANE),
                    0xab => StubAddressing(LXA),
                    0xcb => StubAddressing(SBX),
                    0xeb => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0c  NOP*    BIT     JMP     JMP ()  STY     LDY     CPY     CPX     Absolute
                    0x0c => StubAddressing(NOP),
                    0x2c => StubAddressing(BIT),
                    0x4c => StubAddressing(JMP),
                    0x6c => StubAddressing(JMP),
                    0x8c => StubAddressing(STY),
                    0xac => StubAddressing(LDY),
                    0xcc => StubAddressing(CPY),
                    0xec => StubAddressing(CPX),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute
                    0x0d => StubAddressing(ORA),
                    0x2d => StubAddressing(AND),
                    0x4d => StubAddressing(EOR),
                    0x6d => StubAddressing(ADC),
                    0x8d => StubAddressing(STA),
                    0xad => StubAddressing(LDA),
                    0xcd => StubAddressing(CMP),
                    0xed => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0e  ASL     ROL     LSR     ROR     STX     LDX     DEC     INC     Absolute
                    0x0e => StubAddressing(ASL),
                    0x2e => StubAddressing(ROL),
                    0x4e => StubAddressing(LSR),
                    0x6e => StubAddressing(ROR),
                    0x8e => StubAddressing(STX),
                    0xae => StubAddressing(LDX),
                    0xce => StubAddressing(DEC),
                    0xee => StubAddressing(INC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+0f  SLO*    RLA*    SRE*    RRA*    SAX*    LAX*    DCP*    ISB*    Absolute
                    0x0f => StubAddressing(SLO),
                    0x2f => StubAddressing(RLA),
                    0x4f => StubAddressing(SRE),
                    0x6f => StubAddressing(RRA),
                    0x8f => StubAddressing(SAX),
                    0xaf => StubAddressing(LAX),
                    0xcf => StubAddressing(DCP),
                    0xef => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+10  BPL     BMI     BVC     BVS     BCC     BCS     BNE     BEQ     Relative
                    0x10 => StubAddressing(BPL),
                    0x30 => StubAddressing(BMI),
                    0x50 => StubAddressing(BVC),
                    0x70 => StubAddressing(BVS),
                    0x90 => StubAddressing(BCC),
                    0xb0 => StubAddressing(BCS),
                    0xd0 => StubAddressing(BNE),
                    0xf0 => StubAddressing(BEQ),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+11  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     (indir),y
                    0x11 => StubAddressing(ORA),
                    0x31 => StubAddressing(AND),
                    0x51 => StubAddressing(EOR),
                    0x71 => StubAddressing(ADC),
                    0x91 => StubAddressing(STA),
                    0xb1 => StubAddressing(LDA),
                    0xd1 => StubAddressing(CMP),
                    0xf1 => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+12   t       t       t       t       t       t       t       t         ?
                    0x12 => StubAddressing(STP),
                    0x32 => StubAddressing(STP),
                    0x52 => StubAddressing(STP),
                    0x72 => StubAddressing(STP),
                    0x92 => StubAddressing(STP),
                    0xb2 => StubAddressing(STP),
                    0xd2 => StubAddressing(STP),
                    0xf2 => StubAddressing(STP),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+13  SLO*    RLA*    SRE*    RRA*    SHA**   LAX*    DCP*    ISB*    (indir),y
                    0x13 => StubAddressing(SLO),
                    0x33 => StubAddressing(RLA),
                    0x53 => StubAddressing(SRE),
                    0x73 => StubAddressing(RRA),
                    0x93 => StubAddressing(SHA),
                    0xb3 => StubAddressing(LAX),
                    0xd3 => StubAddressing(DCP),
                    0xf3 => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+14  NOP*    NOP*    NOP*    NOP*    STY     LDY     NOP*    NOP*    Zeropage,x
                    0x14 => StubAddressing(NOP),
                    0x34 => StubAddressing(NOP),
                    0x54 => StubAddressing(NOP),
                    0x74 => StubAddressing(NOP),
                    0x94 => StubAddressing(STY),
                    0xb4 => StubAddressing(LDY),
                    0xd4 => StubAddressing(NOP),
                    0xf4 => StubAddressing(NOP),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+15  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Zeropage,x
                    0x15 => StubAddressing(ORA),
                    0x35 => StubAddressing(AND),
                    0x55 => StubAddressing(EOR),
                    0x75 => StubAddressing(ADC),
                    0x95 => StubAddressing(STA),
                    0xb5 => StubAddressing(LDA),
                    0xd5 => StubAddressing(CMP),
                    0xf5 => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+16  ASL     ROL     LSR     ROR     STX  y) LDX  y) DEC     INC     Zeropage,x
                    0x16 => StubAddressing(ASL),
                    0x36 => StubAddressing(ROL),
                    0x56 => StubAddressing(LSR),
                    0x76 => StubAddressing(ROR),
                    0x96 => StubAddressing(STX),
                    0xb6 => StubAddressing(LDX),
                    0xd6 => StubAddressing(DEC),
                    0xf6 => StubAddressing(INC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+17  SLO*    RLA*    SRE*    RRA*    SAX* y) LAX* y) DCP*    ISB*    Zeropage,x
                    0x17 => StubAddressing(SLO),
                    0x37 => StubAddressing(RLA),
                    0x57 => StubAddressing(SRE),
                    0x77 => StubAddressing(RRA),
                    0x97 => StubAddressing(SAX),
                    0xb7 => StubAddressing(LAX),
                    0xd7 => StubAddressing(DCP),
                    0xf7 => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+18  CLC     SEC     CLI     SEI     TYA     CLV     CLD     SED     Implied
                    0x18 => StubAddressing(CLC),
                    0x38 => StubAddressing(SEC),
                    0x58 => StubAddressing(CLI),
                    0x78 => StubAddressing(SEI),
                    0x98 => StubAddressing(TYA),
                    0xb8 => StubAddressing(CLV),
                    0xd8 => StubAddressing(CLD),
                    0xf8 => StubAddressing(SED),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+19  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,y
                    0x19 => StubAddressing(ORA),
                    0x39 => StubAddressing(AND),
                    0x59 => StubAddressing(EOR),
                    0x79 => StubAddressing(ADC),
                    0x99 => StubAddressing(STA),
                    0xb9 => StubAddressing(LDA),
                    0xd9 => StubAddressing(CMP),
                    0xf9 => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1a  NOP*    NOP*    NOP*    NOP*    TXS     TSX     NOP*    NOP*    Implied
                    0x1a => StubAddressing(NOP),
                    0x3a => StubAddressing(NOP),
                    0x5a => StubAddressing(NOP),
                    0x7a => StubAddressing(NOP),
                    0x9a => StubAddressing(TXS),
                    0xba => StubAddressing(TSX),
                    0xda => StubAddressing(NOP),
                    0xfa => StubAddressing(NOP),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1b  SLO*    RLA*    SRE*    RRA*    SHS**   LAS**   DCP*    ISB*    Absolute,y
                    0x1b => StubAddressing(SLO),
                    0x3b => StubAddressing(RLA),
                    0x5b => StubAddressing(SRE),
                    0x7b => StubAddressing(RRA),
                    0x9b => StubAddressing(SHS),
                    0xbb => StubAddressing(LAS),
                    0xdb => StubAddressing(DCP),
                    0xfb => StubAddressing(ISB),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1c  NOP*    NOP*    NOP*    NOP*    SHY**   LDY     NOP*    NOP*    Absolute,x
                    0x1c => StubAddressing(NOP),
                    0x3c => StubAddressing(NOP),
                    0x5c => StubAddressing(NOP),
                    0x7c => StubAddressing(NOP),
                    0x9c => StubAddressing(SHY),
                    0xbc => StubAddressing(LDY),
                    0xdc => StubAddressing(NOP),
                    0xfc => StubAddressing(NOP),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1d  ORA     AND     EOR     ADC     STA     LDA     CMP     SBC     Absolute,x
                    0x1d => StubAddressing(ORA),
                    0x3d => StubAddressing(AND),
                    0x5d => StubAddressing(EOR),
                    0x7d => StubAddressing(ADC),
                    0x9d => StubAddressing(STA),
                    0xbd => StubAddressing(LDA),
                    0xdd => StubAddressing(CMP),
                    0xfd => StubAddressing(SBC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1e  ASL     ROL     LSR     ROR     SHX**y) LDX  y) DEC     INC     Absolute,x
                    0x1e => StubAddressing(ASL),
                    0x3e => StubAddressing(ROL),
                    0x5e => StubAddressing(LSR),
                    0x7e => StubAddressing(ROR),
                    0x9e => StubAddressing(SHX),
                    0xbe => StubAddressing(LDX),
                    0xde => StubAddressing(DEC),
                    0xfe => StubAddressing(INC),

                    //set  00      20      40      60      80      a0      c0      e0      mode
                    //+1f  SLO*    RLA*    SRE*    RRA*    SHA**y) LAX* y) DCP*    ISB*    Absolute,x
                    0x1f => StubAddressing(SLO),
                    0x3f => StubAddressing(RLA),
                    0x5f => StubAddressing(SRE),
                    0x7f => StubAddressing(RRA),
                    0x9f => StubAddressing(SHA),
                    0xbf => StubAddressing(LAX),
                    0xdf => StubAddressing(DCP),
                    0xff => StubAddressing(ISB),

                    _ => throw new NotImplementedException()
                };

                foreach (var cycle in instructionCycles)
                {
                    yield return cycle;
                }
            }
        }
    }
}
