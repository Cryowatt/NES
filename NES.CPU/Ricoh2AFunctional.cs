using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional : IRicoh2A
    {
        private byte currentOpcode;
        private Address currentOpcodeAddress;
        private Trace cycleTrace = null;
        private InstructionTrace lastInstruction;
        private IBus bus;
        private Address address;
        private Address pointer;
        private Queue<Action<Ricoh2AFunctional>> workQueue = new Queue<Action<Ricoh2AFunctional>>();

        public event Action<InstructionTrace> InstructionTrace;

        public Ricoh2AFunctional(IBus bus)
            : this(bus, new CpuRegisters(StatusFlags.Default | StatusFlags.InterruptDisable))
        { }

        public Ricoh2AFunctional(IBus bus, CpuRegisters registers, Address? initAddress = null)
        {
            this.CycleCount = 5;
            this.bus = bus;
            this.regs = registers;
            this.initAddress = initAddress ?? new Address(0xFFFC);
        }

        public CpuRegisters Registers => (CpuRegisters)this.regs.Clone();

        private CpuRegisters regs;
        private Address initAddress;
        private byte operand;

        private Address Stack => (Address)0x0100 + this.regs.S;

        public long CycleCount { get; private set; }

        private void Enqueue(Action<Ricoh2AFunctional> operation) => this.workQueue.Enqueue(operation);

        private void Enqueue(IEnumerable<Action<Ricoh2AFunctional>> operations)
        {
            foreach (var op in operations)
            {
                this.workQueue.Enqueue(op);
            }
        }

        private static void ReadStackNoOp(Ricoh2AFunctional cpu)
        {
            cpu.Read(cpu.Stack);
        }

        private static void WriteStackFromAddressHigh(Ricoh2AFunctional cpu)
        {
            cpu.Write(cpu.Stack, cpu.address.High);
            cpu.regs.S--;
        }

        private static void WriteStackFromAddressLow(Ricoh2AFunctional cpu)
        {
            cpu.Write(cpu.Stack, cpu.address.Low);
            cpu.regs.S--;
        }

        private static void ReadPCToAddress(Ricoh2AFunctional cpu)
        {
            cpu.address.Ptr = cpu.Read(cpu.regs.PC.Ptr++);
        }

        private static void ReadPCToAddressHigh(Ricoh2AFunctional cpu)
        {
            cpu.address.High = cpu.Read(cpu.regs.PC.Ptr++);
        }

        private static void ReadOperand(Ricoh2AFunctional cpu)
        {
            cpu.operand = cpu.Read(cpu.regs.PC.Ptr++);
        }

        public static IEnumerable<Action<Ricoh2AFunctional>> Operation(Action<Ricoh2AFunctional, byte> operation)
        {
            // Read-only operation
            return new Action<Ricoh2AFunctional>[]
            {
                cpu =>
                {
                    cpu.operand = cpu.Read(cpu.regs.PC++);
                    operation(cpu, cpu.operand);
                    cpu.TraceInstruction(operation.Method.Name, cpu.operand);
                }
            };
        }

        public static IEnumerable<Action<Ricoh2AFunctional>> Operation(Func<Ricoh2AFunctional, byte> operation)
        {
            // Write-only operation
            return new Action<Ricoh2AFunctional>[]
            {
                cpu =>
                {
                    cpu.Write(cpu.address,  operation(cpu));
                    cpu.TraceInstruction(operation.Method.Name, cpu.operand);
                }
            };
        }

        public static IEnumerable<Action<Ricoh2AFunctional>> Operation(Func<Ricoh2AFunctional, byte, byte> operation)
        {
            // Read-Write operation
            return new Action<Ricoh2AFunctional>[]
            {
                cpu =>
                {
                    cpu.operand = cpu.Read(cpu.address);
                },
                cpu=>
                {
                    cpu.Write(cpu.address, cpu.operand);
                    operation(cpu, cpu.operand);
                },
                cpu =>
                {
                    cpu.Write(cpu.address, operation(cpu, cpu.operand));
                    cpu.TraceInstruction(operation.Method.Name, cpu.operand);
                }
            };
        }

        private static void AbsoluteAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional> operation)
        {
            void Work(Ricoh2AFunctional cpu)
            {
                ReadPCToAddressHigh(cpu);
                cpu.regs.PC = cpu.address;
                cpu.TraceInstruction(operation.Method.Name, cpu.address);
            }

            cpu.workQueue.Enqueue(ReadPCToAddress);
            cpu.workQueue.Enqueue(Work);
        }

        private IEnumerable<object> AbsoluteIndirectAddressing(Action<Address> microcode)
        {
            //      2     PC      R  fetch pointer address low, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            //      3     PC      R  fetch pointer address high, increment PC
            pointer.High = Read(this.regs.PC++);
            yield return cycleTrace;

            //      4   pointer   R  fetch low address to latch
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            //      5  pointer+1* R  fetch PCH, copy latch to PCL
            pointer.Low++;
            address.High = Read(pointer);
            microcode(address);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);

            //Note: * The PCH will always be fetched from the same page
            //        than PCL, i.e. page boundary crossing is not handled.
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Action<byte> microcode, byte index)
        {
            // 2     PC      R  fetch low byte of address, increment PC
            address.Ptr = Read(this.regs.PC++);
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
            if (offset > byte.MaxValue)
            {
                Read(address);
                address.Ptr += (ushort)(offset & 0xff00);
                yield return cycleTrace;
            }

            // 5+ address+I  R  re-read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> AbsoluteIndexedAddressing(Func<byte> microcode, byte index)
        {
            // 2     PC      R  fetch low byte of address, increment PC
            address.Ptr = Read(this.regs.PC++);
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

        private IEnumerable<object> AbsoluteIndexedAddressing(Func<byte, byte> microcode, byte index)
        {

            // 2     PC      R  fetch low byte of address, increment PC
            address.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3     PC      R  fetch high byte of address,
            //                  add index register to low address byte,
            //                  increment PC
            address.High = Read(this.regs.PC++);
            var offset = address.Low + index;
            address.Low = (byte)offset;
            yield return cycleTrace;

            // 4  address+X* R  read from effective address,
            //                  fix the high byte of effective address
            var operand = Read(address);
            address.Ptr += (ushort)(offset & 0xff00);
            yield return cycleTrace;

            // 5  address+X  R  re-read from effective address
            operand = Read(address);
            yield return cycleTrace;

            // 6  address+X  W  write the value back to effective address,
            //                  and do the operation on it
            var result = microcode(operand);
            Write(address, operand);
            yield return cycleTrace;

            // 7  address+X  W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);

            //Notes: * The high byte of the effective address may be invalid
            //         at this time, i.e. it may be smaller by $100.
        }

        private static void AccumulatorAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation)
        {
            cpu.Enqueue(cpu =>
            {
                cpu.Read(cpu.regs.PC);
                cpu.regs.A = operation(cpu, cpu.regs.A);
                cpu.TraceInstruction(operation.Method.Name);
            });
        }

        private static void ImmediateAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operation)
        {
            foreach (var operationWork in operation)
            {
                cpu.workQueue.Enqueue(operationWork);
            }
        }

        private static void ImpliedAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional> operation)
        {
            cpu.Enqueue(c =>
            {
                c.Read(c.regs.PC);
                operation(c);
                c.TraceInstruction(operation.Method.Name);
            });
        }

        public IEnumerable<object> IndexedIndirectAddressing(Action<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer.Low += this.regs.X;
            yield return cycleTrace;

            // 4   pointer+X   R  fetch effective address low
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            // 5  pointer+X+1  R  fetch effective address high
            pointer.Low++;
            address.High = Read(pointer);
            yield return cycleTrace;

            // 6    address    R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);

            //Note: The effective address is always fetched from zero page,
            //      i.e. the zero page boundary crossing is not handled.
        }

        public IEnumerable<object> IndexedIndirectAddressing(Func<byte> microcode)
        {

            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer.Low += this.regs.X;
            yield return cycleTrace;

            // 4   pointer+X   R  fetch effective address low
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            // 5  pointer+X+1  R  fetch effective address high
            pointer.Low++;
            address.High = Read(pointer);
            yield return cycleTrace;

            // 6    address    W  write to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndexedIndirectAddressing(Func<byte, byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  read from the address, add X to it
            Read(pointer);
            pointer.Low += this.regs.X;
            yield return cycleTrace;

            // 4   pointer+X   R  fetch effective address low
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            // 5  pointer+X+1  R  fetch effective address high
            pointer.Low++;
            address.High = Read(pointer);
            yield return cycleTrace;

            // 6    address    R  read from effective address
            var operand = Read(address);
            yield return cycleTrace;

            // 7    address    W  write the value back to effective address,
            //                    and do the operation on it
            var result = microcode(operand);
            Write(address, operand);
            yield return cycleTrace;

            // 8    address    W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;

            //Note: The effective address is always fetched from zero page,
            //      i.e. the zero page boundary crossing is not handled.
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndirectIndexedAddressing(Action<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  fetch effective address low
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            pointer.Low++;
            address.High = Read(pointer);
            var low = address.Low + this.regs.Y;
            address.Low = (byte)low;
            yield return cycleTrace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            if (low > byte.MaxValue)
            {
                Read(address);
                address.Ptr += (ushort)(low & 0xff00);
                yield return cycleTrace;
            }

            // 6+  address+Y   R  read from effective address
            var operand = Read(address);
            microcode(operand);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);
        }

        public IEnumerable<object> IndirectIndexedAddressing(Func<byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  fetch effective address low
            address.Ptr = Read(pointer++);
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


        public IEnumerable<object> IndirectIndexedAddressing(Func<byte, byte> microcode)
        {
            // 2      PC       R  fetch pointer address, increment PC
            pointer.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3    pointer    R  fetch effective address low
            address.Ptr = Read(pointer);
            yield return cycleTrace;

            // 4   pointer+1   R  fetch effective address high,
            //                    add Y to low byte of effective address
            pointer.Low++;
            address.High = Read(pointer);
            var low = address.Low + this.regs.Y;
            address.Low = (byte)low;
            yield return cycleTrace;

            // 5   address+Y*  R  read from effective address,
            //                    fix high byte of effective address
            if (low > byte.MaxValue)
            {
                Read(address);
                address.Ptr += (ushort)(low & 0xff00);
                yield return cycleTrace;
            }

            // 6   address+Y   R  read from effective address
            var operand = Read(address);
            yield return cycleTrace;

            // 7   address+Y   W  write the value back to effective address,
            //                    and do the operation on it
            var result = microcode(operand);
            Write(address, operand);
            yield return cycleTrace;

            // 8   address+Y   W  write the new value to effective address
            Write(address, result);
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, pointer);

            //Notes: The effective address is always fetched from zero page,
            //       i.e. the zero page boundary crossing is not handled.

            //       * The high byte of the effective address may be invalid
            //         at this time, i.e. it may be smaller by $100.
        }

        private IEnumerable<object> RelativeAddressing(Func<bool> microcode)
        {
            //2     PC      R  fetch operand, increment PC
            var operand = Read(this.regs.PC++);
            var jumpAddress = this.regs.PC + (sbyte)operand;
            //yield return cycleTrace;

            if (microcode())
            {
                //3     PC      R  Fetch opcode of next instruction,
                //                 If branch is taken, add operand to PCL.
                //                 Otherwise increment PC.
                Read(this.regs.PC);
                this.regs.PC.Low = jumpAddress.Low;
                yield return cycleTrace;

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

        private static void ZeroPageAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operation)
        {
            cpu.workQueue.Enqueue(ReadPCToAddress);
            cpu.Enqueue(operation);
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte> microcode)
        {
            // 2    PC     R  fetch address, increment PC
            address.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3  address  W  write register to effective address
            Write(address, microcode());
            yield return cycleTrace;
            TraceInstruction(microcode.Method.Name, address);
        }

        private IEnumerable<object> ZeroPageAddressing(Func<byte, byte> microcode)
        {
            // 2    PC     R  fetch address, increment PC
            address.Ptr = Read(this.regs.PC++);
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
            address.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3   address R  read from address, add index register to it
            Read(address);
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
            address.Ptr = Read(this.regs.PC++);
            yield return cycleTrace;

            // 3   address   R  read from address, add index register to it
            Read(address);
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
            address.Ptr = Read(this.regs.PC++);
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

        private static void ReadResetToPCL(Ricoh2AFunctional cpu)
        {
            cpu.regs.PC.Low = cpu.Read(cpu.initAddress);
        }

        private static void ReadResetToPCH(Ricoh2AFunctional cpu)
        {
            cpu.regs.PC.High = cpu.Read(cpu.initAddress + 1);
        }

        public void Reset()
        {
            StartTrace();
            workQueue.Enqueue(ReadResetToPCL);
            workQueue.Enqueue(ReadResetToPCH);
            workQueue.Enqueue(QueueOpCode);
        }

        public Trace DoCycle()
        {
            var work = workQueue.Dequeue();
            work(this);
            CycleCount++;
            return cycleTrace;
        }

        [Conditional("DEBUG")]
        private void StartTrace()
        {
            this.cycleTrace = new Trace(this);
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name)
        {
            lastInstruction = new InstructionTrace(currentOpcodeAddress, currentOpcode, name);
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name, Address address)
        {
            lastInstruction = new InstructionTrace(currentOpcodeAddress, currentOpcode, name, address);
        }

        [Conditional("DEBUG")]
        public void TraceInstruction(string name, byte operand)
        {
            lastInstruction = new InstructionTrace(currentOpcodeAddress, currentOpcode, name, operand);
        }

        private byte Read(Address address)
        {
            var result = this.bus.Read(address);
            //Console.WriteLine("{0} => {1}", address, result);
            SetTraceBusAction(address, result, isWrite: false);
            return result;
        }

        private void Write(Address address, byte value)
        {
            //Console.WriteLine("{0} <= {1}", address, value);
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
