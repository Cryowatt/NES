using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NES.CPU
{
    public partial class Ricoh2A
    {
        private byte currentOpcode;
        private Address currentOpcodeAddress;
        private Trace cycleTrace = null;
        private IBus bus;

        public event Action<InstructionTrace> InstructionTrace;

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
            yield return cycleTrace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            yield return cycleTrace;

            // 4  address  R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;

            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteAddressing(Func<byte> microcode)
        {
            // 2    PC     R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            yield return cycleTrace;

            // 4  address  W  write register to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteAddressing(Func<byte, byte> microcode)
        {
            // 2    PC     R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    PC     R  fetch high byte of address, increment PC
            address.High = Read(this.regs.PC++);
            yield return cycleTrace;

            // 4  address  R  read from effective address
            var operand = Read(address);
            yield return cycleTrace;

            // 5  address  W  write the value back to effective address,
            //                and do the operation on it
            Write(address, operand);
            var result = microcode(operand);
            yield return cycleTrace;

            // 6  address  W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteAddressing(Action<Address> microcode)
        {
            // 2    PC     R  fetch low address byte, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;
            // 3    PC     R  copy low address byte to PCL, fetch high address
            //                byte to PCH
            address.High = Read(this.regs.PC++);
            microcode(address);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteIndirectAddressing(Action<Address> microcode)
        {
            //      2     PC      R  fetch pointer address low, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return cycleTrace;

            //      3     PC      R  fetch pointer address high, increment PC
            pointer.High = Read(this.regs.PC++);
            yield return cycleTrace;

            //      4   pointer   R  fetch low address to latch
            Address address = Read(pointer);
            yield return cycleTrace;

            //      5  pointer+1* R  fetch PCH, copy latch to PCL
            address.High = Read(pointer + 1);
            microcode(address);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Action<byte> microcode, byte index)
        {
            // 2     PC      R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3     PC      R  fetch high byte of address,
            //                  add index register to low address byte,
            //                  increment PC
            address.High = Read(this.regs.PC++);
            var offset = address.Low + index;
            address.Low = (byte)offset;
            yield return cycleTrace;

            // 4  address+I* R  read from effective address,
            //                  fix the high byte of effective address
            Read(address);
            address.Ptr += (ushort)(offset & 0xff00);
            yield return cycleTrace;

            // 5+ address+I  R  re-read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Func<byte> microcode, byte index)
        {
            // 2     PC      R  fetch low byte of address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3     PC      R  fetch high byte of address,
            //                  add index register to low address byte,
            //                  increment PC
            address.High = Read(this.regs.PC++);
            var offset = address.Low + index;
            address.Low = (byte)offset;
            yield return cycleTrace;

            // 4  address+I* R  read from effective address,
            //                  fix the high byte of effective address
            Read(address);
            address.Ptr += (ushort)(offset & 0xff00);
            yield return cycleTrace;

            //5  address+I  W  write to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AccumulatorAddressing(Func<byte, byte> microcode)
        {
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            this.regs.A = microcode(this.regs.A);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name);
        }

        private IEnumerable<object> ImmediateAddressing(Action<byte> microcode)
        {
            //2    PC     R  fetch value, increment PC
            var operand = Read(this.regs.PC++);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, operand);
        }

        private IEnumerable<object> ImpliedAddressing(Action microcode)
        {
            //PC:R  read next instruction byte(and throw it away)
            Read(this.regs.PC);
            microcode();
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name);
        }

        public IEnumerable<object> IndexedIndirectAddressing(Action<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer += this.regs.X;
            yield return cycleTrace;

            // 4   pointer+X   R  fetch effective address low
            Address address = Read(pointer);
            yield return cycleTrace;

            // 5  pointer+X+1  R  fetch effective address high
            address.High = Read(pointer + 1);
            yield return cycleTrace;

            // 6    address    R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndexedIndirectAddressing(Func<byte> microcode)
        {

            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return cycleTrace;
            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer += this.regs.X;
            yield return cycleTrace;

            // 4   pointer+X   R  fetch effective address low
            Address address = Read(pointer);
            yield return cycleTrace;

            // 5  pointer+X+1  R  fetch effective address high
            address.High = Read(pointer + 1);
            yield return cycleTrace;

            // 6    address    W  write to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndirectIndexedAddressing(Action<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  fetch effective address low
            yield return cycleTrace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            Address address = Read(pointer);
            address.High = Read(pointer + 1);
            var low = address.Low + this.regs.Y;
            address.Low += (byte)low;
            yield return cycleTrace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            Read(address);
            address.Ptr += (ushort)(low & 0xff00);
            yield return cycleTrace;

            // 6+  address+Y   R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndirectIndexedAddressing(Func<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            Address pointer = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  fetch effective address low
            Address address = Read(pointer++);
            yield return cycleTrace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            address.High = Read(pointer);
            var low = address.Low + this.regs.Y;
            address.Low += (byte)low;
            yield return cycleTrace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            Read(address);
            address.Ptr += (ushort)(low & 0xff00);
            yield return cycleTrace;

            // 6   address+Y   W  write to effective address
            Write(address, microcode());
            yield return cycleTrace;

            //Notes: The effective address is always fetched from zero page,
            //       i.e. the zero page boundary crossing is not handled.

            //       * The high byte of the effective address may be invalid
            //         at this time, i.e. it may be smaller by $100.
            TraceInstruction(microcode.Method.Name, pointer);
        }

        private IEnumerable<object> RelativeAddressing(Func<bool> microcode)
        {
            //2     PC      R  fetch operand, increment PC
            var operand = Read(this.regs.PC++);
            var jumpAddress = this.regs.PC + (sbyte)operand;
            //yield return trace;

            if (microcode())
            {
                //3     PC      R  Fetch opcode of next instruction,
                //                 If branch is taken, add operand to PCL.
                //                 Otherwise increment PC.
                //Read(this.regs.PC);
                this.regs.PC.Low = jumpAddress.Low;
                //yield return trace;

                if (this.regs.PC.High != jumpAddress.High)
                {
                    //4+    PC*     R  Fetch opcode of next instruction.
                    //                 Fix PCH. If it did not change, increment PC.
                    Read(this.regs.PC);
                    this.regs.PC.High = jumpAddress.High;
                    yield return cycleTrace;
                }
            }

            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, jumpAddress);
        }

        private IEnumerable<object> ZeroPageAddressing(Action<byte> microcode)
        {
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3  address  R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte> microcode)
        {
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3  address  W  write register to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte, byte> microcode)
        {
            // 2    PC     R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3  address  R  read from effective address
            var operand = Read(address);
            yield return cycleTrace;

            // 4  address  W  write the value back to effective address,
            //                and do the operation on it
            Write(address, operand);
            var result = microcode(operand);
            yield return cycleTrace;

            // 5  address  W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageIndexedAddressing(Action<byte> microcode, byte index)
        {
            // 2     PC R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3   address R  read from address, add index register to it
            Read((Address)address);
            address.Low += index;
            yield return cycleTrace;

            // 4  address+I* R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageIndexedAddressing(Func<byte> microcode, byte index)
        {
            // 2     PC      R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3   address   R  read from address, add index register to it
            Read((Address)address);
            address.Low += index;
            yield return cycleTrace;

            // 4  address+I* W  write to effective address
            Write(address, microcode());
            yield return cycleTrace;

            //Notes: I denotes either index register (X or Y).

            //       * The high byte of the effective address is always zero,
            //         i.e. page boundary crossings are not handled.
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageIndexedAddressing(Func<byte, byte> microcode, byte index)
        {
            // 2     PC      R  fetch address, increment PC
            Address address = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3   address   R  read from address, add index register to it
            Read((Address)address);
            address.Low += index;
            yield return cycleTrace;

            // 4  address+X* R  read from effective address
            var operand = Read(address);
            yield return cycleTrace;

            // 5  address+X* W  write the value back to effective address,
            //                  and do the operation on it
            Write(address, operand);
            var result = microcode(operand);
            yield return cycleTrace;

            // 6  address+X* W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;

            //Note: * The high byte of the effective address is always zero,
            //        i.e. page boundary crossings are not handled.
            TraceInstruction(microcode.Method.Name, address);
        }

        public IEnumerable<Trace> Process()
        {
            StartTrace();
            this.regs.PC.Low = Read(0xFFFC);
            yield return cycleTrace;
            this.regs.PC.High = Read(0xFFFD);
            yield return cycleTrace;

            while (true)
            {
                // PC:R  fetch opcode, increment PC
                currentOpcodeAddress = this.regs.PC;
                currentOpcode = Read(this.regs.PC++);
                IEnumerable<object> instructionCycles = GetMicrocode(currentOpcode);
                yield return cycleTrace;


                foreach (var cycle in instructionCycles)
                {
                    yield return cycle as Trace;
                }
            }
        }

        [Conditional("DEBUG")]
        private void StartTrace()
        {
            this.cycleTrace = new Trace(this);
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name)
        {
            InstructionTrace?.Invoke(new InstructionTrace(currentOpcodeAddress, currentOpcode, name));
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name, Address address)
        {
            InstructionTrace?.Invoke(new InstructionTrace(currentOpcodeAddress, currentOpcode, name, address));
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name, byte operand)
        {
            InstructionTrace?.Invoke(new InstructionTrace(currentOpcodeAddress, currentOpcode, name, operand));
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
            this.cycleTrace.BusAddress = address;
            this.cycleTrace.BusValue = result;
            this.cycleTrace.BusWrite = isWrite;
        }
    }
}
