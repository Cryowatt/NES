using System;
using System.Collections;
using System.Collections.Generic;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        private IBus bus;

        public Ricoh2A(IBus bus)
            : this(bus, new CpuRegisters(StatusFlags.Default | StatusFlags.InterruptDisable))
        { }

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

        private IEnumerable<object> RelativeAddressing(Func<bool> microcode)
        {
            //2     PC      R  fetch operand, increment PC
            var operand = Read(this.regs.PC++);
            yield return null;
            //3     PC      R  Fetch opcode of next instruction,
            //                 If branch is taken, add operand to PCL.
            //                 Otherwise increment PC.
            Read(this.regs.PC);
            var jumpAddress = (Address)(this.regs.PC + (sbyte)operand);
            if (!microcode())
            {
                this.regs.PC++;
                yield return null;
            }
            else
            {
                this.regs.PC.Low = jumpAddress.Low;
                yield return null;

                //4+    PC*     R  Fetch opcode of next instruction.
                //                 Fix PCH. If it did not change, increment PC.
                Read(this.regs.PC);
                yield return null;

                if (this.regs.PC.High == jumpAddress.High)
                {
                    this.regs.PC++;
                }
                else
                {
                    this.regs.PC.High = jumpAddress.High;
                    //5!    PC      R  Fetch opcode of next instruction,
                    //                 increment PC.
                    Read(this.regs.PC++);
                    yield return null;
                }
            }
        }

        public IEnumerable<object> Process()
        {
            //$4017 = $00 (frame irq enabled)
            this.bus.Write(0x4017, 0);
            yield return null;
            //$4015 = $00 (all channels disabled)
            this.bus.Write(0x4015, 0);
            yield return null;

            //$4000-$400F = $00
            for (Address address = 0x4000; address <= 0x400f; address++)
            {
                this.bus.Write(address, 0);
                yield return null;
            }

            //$4010-$4013 = $00 [6]
            for (Address address = 0x4010; address <= 0x4013; address++)
            {
                this.bus.Write(address, 0);
                yield return null;
            }

            // TODO: read this and do something, probably.
            //All 15 bits of noise channel LFSR = $0000[7]. The first time the LFSR is clocked from the all-0s state, it will shift in a 1.
            //2A03G: APU Frame Counter reset. (but 2A03letterless: APU frame counter powers up at a value equivalent to 15)
            //Internal memory ($0000-$07FF) has unreliable startup state. Some machines may have consistent RAM contents at power-on, but others do not.
            //Emulators often implement a consistent RAM startup state (e.g. all $00 or $FF, or a particular pattern), and flash carts like the PowerPak may partially or fully initialize RAM before starting a program, so an NES programmer must be careful not to rely on the startup contents of RAM.

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
