using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NES.CPU
{
    public partial class Ricoh2AFunctional : IRicoh2A
    {
        private byte currentOpcode;
        private Address currentOpcodeAddress;
        private Trace cycleTrace = null;
        private InstructionTrace lastInstruction;
        private IBus bus;
        private Address stackPointer = 0x0100;
        private Address address;
        private Address pointer;
        private Queue<Action<Ricoh2AFunctional>> workQueue = new Queue<Action<Ricoh2AFunctional>>(8);
        private CpuRegisters regs;
        private Address initAddress;
        private byte operand;

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

        private Address Stack
        {
            get
            {
                stackPointer.Low = this.regs.S;
                return stackPointer;
            }
        }

        public long CycleCount { get; private set; }

        private void Enqueue(Action<Ricoh2AFunctional> operation) => this.workQueue.Enqueue(operation);

        private static void ReadPCNoOp(Ricoh2AFunctional cpu)
        {
            cpu.Read(cpu.regs.PC);
        }

        private static void ReadStackNoOp(Ricoh2AFunctional cpu)
        {
            cpu.Read(cpu.Stack);
        }

        private static void PushStackFromPCH(Ricoh2AFunctional cpu)
        {
            cpu.Write(cpu.Stack, cpu.regs.PC.High);
            cpu.regs.S--;
        }

        private static void PushStackFromPCL(Ricoh2AFunctional cpu)
        {
            cpu.Write(cpu.Stack, cpu.regs.PC.Low);
            cpu.regs.S--;
        }

        private static void PushStackFromP(Ricoh2AFunctional cpu)
        {
            cpu.Write(cpu.Stack, (byte)cpu.regs.P);
            cpu.regs.S--;
        }

        private static void ReadPCToAddress(Ricoh2AFunctional cpu)
        {
            cpu.address.Ptr = cpu.Read(cpu.regs.PC);
            cpu.regs.PC.Ptr++;
        }

        private static void ReadPCToAddressHigh(Ricoh2AFunctional cpu)
        {
            cpu.address.High = cpu.Read(cpu.regs.PC);
            cpu.regs.PC.Ptr++;
        }

        private static void ReadOperand(Ricoh2AFunctional cpu)
        {
            cpu.operand = cpu.Read(cpu.regs.PC);
            cpu.regs.PC.Ptr++;
        }

        public void QueueOperation(Action<Ricoh2AFunctional, byte> operation)
        {
            this.Enqueue(cpu =>
            {
                cpu.operand = cpu.Read(cpu.address);
                operation(cpu, cpu.operand);
                cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
            });
        }

        public void QueueOperation(Func<Ricoh2AFunctional, byte> operation)
        {
            this.Enqueue(cpu =>
            {
                cpu.Write(cpu.address, operation(cpu));
                cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
            });
        }
        public void QueueOperation(Func<Ricoh2AFunctional, byte, byte> operation)
        {
            this.Enqueue(cpu =>
            {
                cpu.operand = cpu.Read(cpu.address);
            });
            this.Enqueue(cpu =>
            {
                cpu.Write(cpu.address, cpu.operand);
            });
            this.Enqueue(cpu =>
            {
                cpu.Write(cpu.address, operation(cpu, cpu.operand));
                cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
            });
        }

        public void AbsoluteAddressing(Action<Ricoh2AFunctional, byte> operation) =>
            AbsoluteAddressing(() => QueueOperation(operation));
        public void AbsoluteAddressing(Func<Ricoh2AFunctional, byte> operation) =>
            AbsoluteAddressing(() => QueueOperation(operation));
        public void AbsoluteAddressing(Func<Ricoh2AFunctional, byte, byte> operation) =>
            AbsoluteAddressing(() => QueueOperation(operation));

        private void AbsoluteAddressing(Action queueOperation)
        {
            Enqueue(ReadPCToAddress);
            Enqueue(ReadPCToAddressHigh);
            queueOperation();
        }

        private void AbsoluteAddressing(Action<Ricoh2AFunctional> operation)
        {
            void Work(Ricoh2AFunctional cpu)
            {
                ReadPCToAddressHigh(cpu);
                cpu.regs.PC = cpu.address;
                cpu.TraceInstruction(operation.Method.Name, cpu.address);
            }

            Enqueue(ReadPCToAddress);
            Enqueue(Work);
        }

        private void AbsoluteIndirectAddressing(Action<Ricoh2AFunctional> operation)
        {
            Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            Enqueue(c => c.pointer.High = c.Read(c.regs.PC++));
            Enqueue(c => c.address.Ptr = c.Read(c.pointer));
            Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                operation(c);
                c.TraceInstruction(operation.Method.Name, c.address);
            });
        }

        private void AbsoluteIndexedAddressing(Action<Ricoh2AFunctional, byte> operation, byte index)
        {
            Enqueue(c => c.address.Ptr = c.Read(c.regs.PC++));
            Enqueue(c =>
            {
                c.address.High = c.Read(c.regs.PC++);
                c.pointer.Ptr = (ushort)(c.address.Ptr + index);
                c.address.Low = c.pointer.Low;
            });
            Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                if (c.address.High == c.pointer.High)
                {
                    operation(c, c.operand);
                }
                else
                {
                    c.address.High = c.pointer.High;
                    c.QueueOperation(operation);
                }
            });
        }
        private void AbsoluteIndexedAddressing(Func<Ricoh2AFunctional, byte> operation, byte index) =>
            AbsoluteIndexedAddressing(() => QueueOperation(operation), index);
        private void AbsoluteIndexedAddressing(Func<Ricoh2AFunctional, byte, byte> operation, byte index) =>
            AbsoluteIndexedAddressing(() => QueueOperation(operation), index);

        private void AbsoluteIndexedAddressing(Action queueOperation, byte index)
        {
            Enqueue(ReadPCToAddress);
            Enqueue(c =>
            {
                c.address.High = c.Read(c.regs.PC);
                c.regs.PC.Ptr++;
                c.pointer.Ptr = (ushort)(c.address.Ptr + index);
                c.address.Low = c.pointer.Low;
            });
            Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                c.address.High = c.pointer.High;
            });
            queueOperation();
        }

        private void AccumulatorAddressing(Func<Ricoh2AFunctional, byte, byte> operation)
        {
            Enqueue(cpu =>
            {
                cpu.Read(cpu.regs.PC);
                cpu.regs.A = operation(cpu, cpu.regs.A);
                cpu.TraceInstruction(operation.Method.Name);
            });
        }

        private void ImmediateAddressing(Action<Ricoh2AFunctional, byte> operation)
        {
            Enqueue(c =>
            {
                c.operand = c.Read(c.regs.PC);
                c.regs.PC++;
                operation(c, c.operand);
                c.TraceInstruction(operation.Method.Name, c.operand);
            });
        }

        private void ImpliedAddressing(Action<Ricoh2AFunctional> operation)
        {
            Enqueue(c =>
            {
                c.Read(c.regs.PC);
                operation(c);
                c.TraceInstruction(operation.Method.Name);
            });
        }

        public void IndexedIndirectAddressing(Action<Ricoh2AFunctional, byte> operation) =>
            IndexedIndirectAddressing(() => QueueOperation(operation));
        public void IndexedIndirectAddressing(Func<Ricoh2AFunctional, byte> operation) =>
            IndexedIndirectAddressing(() => QueueOperation(operation));
        public void IndexedIndirectAddressing(Func<Ricoh2AFunctional, byte, byte> operation) =>
            IndexedIndirectAddressing(() => QueueOperation(operation));

        public void IndexedIndirectAddressing(Action queueOperation)
        {
            Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            Enqueue(c =>
            {
                c.Read(c.pointer);
                c.pointer.Low += c.regs.X;
            });
            Enqueue(c => c.address.Ptr = c.Read(c.pointer));
            Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
            });
            queueOperation();
        }

        public void IndirectIndexedAddressing(Action<Ricoh2AFunctional, byte> operation)
        {
            Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            Enqueue(c =>
            {
                c.address.Ptr = c.Read(c.pointer);
            });
            Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                c.pointer.Ptr = (ushort)(c.address.Ptr + c.regs.Y);
                c.address.Low = c.pointer.Low;
            });
            Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                if (c.address.High == c.pointer.High)
                {
                    operation(c, c.operand);
                }
                else
                {
                    c.address.High = c.pointer.High;
                    c.QueueOperation(operation);
                }
            });
        }

        public void IndirectIndexedAddressing(Func<Ricoh2AFunctional, byte> operation) =>
            IndirectIndexedAddressing(() => QueueOperation(operation));

        public void IndirectIndexedAddressing(Func<Ricoh2AFunctional, byte, byte> operation) =>
            IndirectIndexedAddressing(() => QueueOperation(operation));

        private void IndirectIndexedAddressing(Action queueOperation)
        {
            Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            Enqueue(c =>
            {
                c.address.Ptr = c.Read(c.pointer);
            });
            Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                c.pointer.Ptr = (ushort)(c.address.Ptr + c.regs.Y);
                c.address.Low = c.pointer.Low;
            });
            Enqueue(c =>
            {
                c.Read(c.address);
                c.address.High = c.pointer.High;
            });
            queueOperation();
        }

        private void RelativeAddressing(Func<Ricoh2AFunctional, bool> operation)
        {
            void Conditional(Ricoh2AFunctional cpu)
            {
                ReadOperand(cpu);
                cpu.address.Ptr = (ushort)(cpu.regs.PC.Ptr + (sbyte)cpu.operand);

                if (operation(cpu))
                {
                    cpu.Enqueue(Jump);
                }
                else
                {
                    cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
                }
            }

            void Jump(Ricoh2AFunctional cpu)
            {
                ReadPCNoOp(cpu);
                cpu.regs.PC.Low = cpu.address.Low;

                if (cpu.regs.PC.High != cpu.address.High)
                {
                    cpu.Enqueue(FixHigh);
                }
                else
                {
                    cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
                }
            }

            void FixHigh(Ricoh2AFunctional cpu)
            {
                cpu.Read(cpu.regs.PC);
                cpu.regs.PC.High = cpu.address.High;
                cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
            }

            Enqueue(Conditional);
        }

        private void ZeroPageAddressing(Action<Ricoh2AFunctional, byte> operation) =>
            ZeroPageAddressing(() => QueueOperation(operation));
        private void ZeroPageAddressing(Func<Ricoh2AFunctional, byte> operation) =>
            ZeroPageAddressing(() => QueueOperation(operation));
        private void ZeroPageAddressing(Func<Ricoh2AFunctional, byte, byte> operation) =>
            ZeroPageAddressing(() => QueueOperation(operation));

        private void ZeroPageAddressing(Action queueOperation)
        {
            Enqueue(ReadPCToAddress);
            queueOperation();
        }

        private void ZeroPageIndexedAddressing(Action<Ricoh2AFunctional, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(() => QueueOperation(operation), index);
        private void ZeroPageIndexedAddressing(Func<Ricoh2AFunctional, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(() => QueueOperation(operation), index);
        private void ZeroPageIndexedAddressing(Func<Ricoh2AFunctional, byte, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(() => QueueOperation(operation), index);

        private void ZeroPageIndexedAddressing(Action queueOperation, byte index)
        {
            Enqueue(ReadPCToAddress);
            Enqueue(c => c.address.Low += index);
            queueOperation();
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
        }

        public Trace DoCycle()
        {
            if (workQueue.Count == 0)
            {
                Enqueue(cpu => QueueOpCode());
            }

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

        [Conditional("DEBUG")]
        public void TraceInstruction(string name, Address address, byte operand)
        {
            lastInstruction = new InstructionTrace(currentOpcodeAddress, currentOpcode, name, address, operand);
        }

        private byte Read(Address address)
        {
            var value = this.bus.Read(address);
            SetTraceBusAction(address, value, isWrite: false);
            return value;
        }

        private void Write(Address address, byte value)
        {
            this.bus.Write(address, value);
            SetTraceBusAction(address, value, isWrite: true);
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
