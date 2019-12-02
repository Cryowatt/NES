﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NES.CPU
{
    public class Trace
    {
        private const string WriteSymbol = "<=";
        private const string ReadSymbol = "=>";
        public byte OpCode { get; set; }
        public byte? Operand { get; set; }
        public byte Value { get; set; }
        public Address? AddressOperand { get; set; }
        public CpuRegisters Registers => this.cpu.Registers;

        private readonly Ricoh2A cpu;

        public Trace(Ricoh2A cpu)
        {
            this.cpu = cpu;
        }

        public Address PC { get; internal set; }
        public Address BusAddress { get; internal set; }
        public byte BusValue { get; internal set; }
        public bool BusWrite { get; internal set; }
        public string OpCodeMethod { get; internal set; }

        public override string ToString()
        {
            //return "F381  C8        INY                             A:1B X:02 Y:EE P:67 SP:FB PPU: 70,208 CYC:23673";
            return $"{PC}  {OpCode:X2}  {OpCodeMethod ?? "???"} {Operand ?? AddressOperand} = {Value}\t{BusAddress} {(BusWrite ? WriteSymbol : ReadSymbol)} #{BusValue:X2} \t{Registers}";
        }
    }
}
