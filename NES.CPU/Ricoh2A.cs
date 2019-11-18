using System;
using System.Collections;
using System.Collections.Generic;

namespace NES.CPU
{
    public partial class Ricoh2A
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

        public void STP()
        {
            throw new InvalidOperationException();
        }

        // This will be deleted later
        private IEnumerable<object> StubAddressing(Action microcode) => throw new NotImplementedException();
        private IEnumerable<object> StubAddressing(Action<byte> microcode) => throw new NotImplementedException();
        private IEnumerable<object> StubAddressing(Func<byte, byte> microcode) => throw new NotImplementedException();

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
                IEnumerable<object> instructionCycles = GetMicrocode(opcode);

                foreach (var cycle in instructionCycles)
                {
                    yield return cycle;
                }
            }
        }

        private byte Read(Address address)
        {
            return 0;
        }
    }
}
