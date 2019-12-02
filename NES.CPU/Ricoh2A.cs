using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        private Trace trace = null;
        private IBus bus;

        public Ricoh2A(IBus bus)
            : this(bus, new CpuRegisters(StatusFlags.Default | StatusFlags.InterruptDisable))
        { }

        public Ricoh2A(IBus bus, CpuRegisters registers)
        {
            this.bus = bus;
            this.regs = registers;
        }

        public CpuRegisters Registers => this.regs;

        private CpuRegisters regs;

        private Address Stack => (Address)0x0100 + this.regs.S;

        public void STP()
        {
            throw new InvalidOperationException();
        }

        // This will be deleted later
        private IEnumerable<object> StubAddressing(Action microcode) => throw new NotImplementedException(microcode.Method.Name);
        private IEnumerable<object> StubAddressing(Action<byte> microcode) => throw new NotImplementedException(microcode.Method.Name);
        private IEnumerable<object> StubAddressing(Func<byte> microcode) => throw new NotImplementedException(microcode.Method.Name);
        private IEnumerable<object> StubAddressing(Func<byte, byte> microcode) => throw new NotImplementedException(microcode.Method.Name);

        private IEnumerable<object> AbsoluteAddressing(Action<byte> microcode)
        {
            // 2    PC     R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            SetTraceOpDetails(microcode.Method.Name, address);
            yield return trace;

            // 4  address  R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        private IEnumerable<object> AbsoluteAddressing(Func<byte> microcode)
        {
            // 2    PC     R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            SetTraceOpDetails(microcode.Method.Name, address);
            yield return trace;

            // 4  address  W  write register to effective address
            Write(address, microcode());
            yield return trace;
        }

        private IEnumerable<object> AbsoluteAddressing(Func<byte, byte> microcode)
        {
            // 2    PC     R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            SetTraceOpDetails(microcode.Method.Name, address);
            yield return trace;

            // 4  address  R  read from effective address
            var operand = Read(address);
            yield return trace;

            // 5  address  W  write the value back to effective address,
            //                and do the operation on it
            Write(address, operand);
            var result = microcode(operand);
            yield return trace;

            // 6  address  W  write the new value to effective address
            Write(address, result);
            yield return trace;
        }

        private IEnumerable<object> AbsoluteAddressing(Action<Address> microcode)
        {
            // 2    PC     R  fetch low address byte, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;
            // 3    PC     R  copy low address byte to PCL, fetch high address
            //                byte to PCH
            address.High = Read(this.regs.PC++);
            SetTraceOpDetails(microcode.Method.Name, address);
            microcode(address);
            yield return trace;
        }

        private IEnumerable<object> AbsoluteIndirectAddressing(Action<Address> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            //      2     PC      R  fetch pointer address low, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return trace;

            //      3     PC      R  fetch pointer address high, increment PC
            pointer.High = Read(this.regs.PC++);
            yield return trace;

            //      4   pointer   R  fetch low address to latch
            Address address = Read(pointer);
            yield return trace;

            //      5  pointer+1* R  fetch PCH, copy latch to PCL
            address.High = Read(pointer + 1);
            microcode(address);
            yield return trace;
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Action<byte> microcode, byte index)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2     PC      R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3     PC      R  fetch high byte of address,
            //                  add index register to low address byte,
            //                  increment PC
            address.High = Read(this.regs.PC++);
            var offset = address.Low + index;
            address.Low = (byte)offset;
            yield return trace;

            // 4  address+I* R  read from effective address,
            //                  fix the high byte of effective address
            Read(address);
            address.Ptr += (ushort)(offset & 0xff00);
            yield return trace;

            // 5+ address+I  R  re-read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Func<byte> microcode, byte index)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2     PC      R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3     PC      R  fetch high byte of address,
            //                  add index register to low address byte,
            //                  increment PC
            address.High = Read(this.regs.PC++);
            var offset = address.Low + index;
            address.Low = (byte)offset;
            yield return trace;

            // 4  address+I* R  read from effective address,
            //                  fix the high byte of effective address
            Read(address);
            address.Ptr += (ushort)(offset & 0xff00);
            yield return trace;

            //5  address+I  W  write to effective address
            Write(address, microcode());
            yield return trace;
        }

        private IEnumerable<object> AccumulatorAddressing(Func<byte, byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            this.regs.A = microcode(this.regs.A);
            yield return trace;
        }

        private IEnumerable<object> ImmediateAddressing(Action<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            //2    PC     R  fetch value, increment PC
            var operand = Read(this.regs.PC++);
            microcode(operand);
            yield return trace;
        }

        private IEnumerable<object> ImpliedAddressing(Action microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            microcode();
            yield return trace;
        }

        public IEnumerable<object> IndirectXAddressing(Action<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return trace;

            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer += this.regs.X;
            yield return trace;

            // 4   pointer+X   R  fetch effective address low
            Address address = Read(pointer);
            yield return trace;

            // 5  pointer+X+1  R  fetch effective address high
            address.High = Read(pointer + 1);
            yield return trace;

            // 6    address    R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        public IEnumerable<object> IndirectYAddressing(Action<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return trace;

            // 3    pointer    R  fetch effective address low
            yield return trace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            Address address = Read(pointer);
            address.High = Read(pointer + 1);
            var low = address.Low + this.regs.Y;
            address.Low += (byte)low;
            yield return trace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            Read(address);
            address.Ptr += (ushort)(low & 0xff00);
            yield return trace;

            // 6+  address+Y   R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        public IEnumerable<object> IndirectYAddressing(Func<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return trace;

            // 3    pointer    R  fetch effective address low
            Address address = Read(pointer++);
            yield return trace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            address.High = Read(pointer);
            var low = address.Low + this.regs.Y;
            address.Low += (byte)low;
            yield return trace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            Read(address);
            address.Ptr += (ushort)(low & 0xff00);
            yield return trace;

            // 6   address+Y   W  write to effective address
            Write(address, microcode());
            yield return trace;

            //Notes: The effective address is always fetched from zero page,
            //       i.e. the zero page boundary crossing is not handled.

            //       * The high byte of the effective address may be invalid
            //         at this time, i.e. it may be smaller by $100.
        }

        private IEnumerable<object> RelativeAddressing(Func<bool> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            //2     PC      R  fetch operand, increment PC
            var operand = Read(this.regs.PC++);
            SetTraceOperand(operand);
            //yield return trace;

            if (microcode())
            {
                //3     PC      R  Fetch opcode of next instruction,
                //                 If branch is taken, add operand to PCL.
                //                 Otherwise increment PC.
                //Read(this.regs.PC);
                var jumpAddress = (Address)(this.regs.PC + (sbyte)operand);
                this.regs.PC.Low = jumpAddress.Low;
                //yield return trace;

                if (this.regs.PC.High != jumpAddress.High)
                {
                    //4+    PC*     R  Fetch opcode of next instruction.
                    //                 Fix PCH. If it did not change, increment PC.
                    Read(this.regs.PC);
                    this.regs.PC.High = jumpAddress.High;
                    yield return trace;
                }
            }

            yield return trace;
        }

        private IEnumerable<object> ZeroPageAddressing(Action<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3  address  R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3  address  W  write register to effective address
            Write(address, microcode());
            yield return trace;
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte, byte> microcode)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3  address  R  read from effective address
            var operand = Read(address);
            yield return trace;

            // 4  address  W  write the value back to effective address,
            //                and do the operation on it
            Write(address, operand);
            var result = microcode(operand);
            yield return trace;

            // 5  address  W  write the new value to effective address
            Write(address, result);
            yield return trace;
        }

        private IEnumerable<object> ZeroPageIndexedAddressing(Action<byte> microcode, byte index)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2     PC R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3   address R  read from address, add index register to it
            Read((Address)address);
            address.Low += index;
            yield return trace;

            // 4  address+I* R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return trace;
        }

        private IEnumerable<object> ZeroPageIndexedAddressing(Func<byte> microcode, byte index)
        {
            SetTraceOpcodeMethod(microcode.Method);
            // 2     PC      R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return trace;

            // 3   address   R  read from address, add index register to it
            Read((Address)address);
            address.Low += index;
            yield return trace;

            // 4  address+I* W  write to effective address
            Write(address, microcode());
            yield return trace;

            //Notes: I denotes either index register (X or Y).

            //       * The high byte of the effective address is always zero,
            //         i.e. page boundary crossings are not handled.
        }

        public IEnumerable<Trace> Process()
        {
            StartTrace();
            //$4017 = $00 (frame irq enabled)
            this.Write(0x4017, 0);
            yield return trace;
            //$4015 = $00 (all channels disabled)
            this.Write(0x4015, 0);
            yield return trace;

            //$4000-$400F = $00
            for (Address address = 0x4000; address <= 0x400f; address++)
            {
                this.Write(address, 0);
                yield return trace;
            }

            //$4010-$4013 = $00 [6]
            for (Address address = 0x4010; address <= 0x4013; address++)
            {
                this.Write(address, 0);
                yield return trace;
            }

            // TODO: read this and do something, probably.
            //All 15 bits of noise channel LFSR = $0000[7]. The first time the LFSR is clocked from the all-0s state, it will shift in a 1.
            //2A03G: APU Frame Counter reset. (but 2A03letterless: APU frame counter powers up at a value equivalent to 15)
            //Internal memory ($0000-$07FF) has unreliable startup state. Some machines may have consistent RAM contents at power-on, but others do not.
            //Emulators often implement a consistent RAM startup state (e.g. all $00 or $FF, or a particular pattern), and flash carts like the PowerPak may partially or fully initialize RAM before starting a program, so an NES programmer must be careful not to rely on the startup contents of RAM.

            this.regs.PC.Low = Read(0xFFFC);
            yield return trace;
            this.regs.PC.High = Read(0xFFFD);
            yield return trace;

            while (true)
            {
                // PC:R  fetch opcode, increment PC
                byte opcode = Read(this.regs.PC);
                SetTraceOpcode(this.regs.PC, opcode);
                this.regs.PC++;
                IEnumerable<object> instructionCycles = GetMicrocode(opcode);
                yield return trace;


                foreach (var cycle in instructionCycles)
                {
                    yield return cycle as Trace;
                }
            }
        }

        [Conditional("DEBUG")]
        private void StartTrace()
        {
            this.trace = new Trace(this);
        }

        [Conditional("DEBUG")]
        private void SetTraceOpcode(Address pc, byte opcode)
        {
            this.trace.OpCode = opcode;
            this.trace.PC = pc;
        }

        [Conditional("DEBUG")]
        private void SetTraceOpDetails(string method)
        {
            this.trace.OpCodeMethod = method;
            this.trace.Operand = null;
            this.trace.AddressOperand = null;
        }

        [Conditional("DEBUG")]
        private void SetTraceOpDetails(string method, Address operand)
        {
            this.trace.OpCodeMethod = method;
            this.trace.Operand = null;
            this.trace.AddressOperand = operand;
        }

        [Conditional("DEBUG")]
        private void SetTraceOpDetails(string method, byte operand)
        {
            this.trace.OpCodeMethod = method;
            this.trace.Operand = operand;
            this.trace.AddressOperand = null;
        }

        private byte Read(Address address)
        {
            var result = this.bus.Read(address);
            SetTraceBusAction(address, result, isWrite: false);
            return result;
        }

        private void Write(Address address, byte value)
        {
            SetTraceBusAction(address, value, isWrite: true);
            this.bus.Write(address, value);
        }

        [Conditional("DEBUG")]
        private void SetTraceBusAction(Address address, byte result, bool isWrite)
        {
            this.trace.BusAddress = address;
            this.trace.BusValue = result;
            this.trace.BusWrite = isWrite;
        }
    }
}
