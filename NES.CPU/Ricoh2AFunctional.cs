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
                    cpu.operand = cpu.Read(cpu.address);
                    operation(cpu, cpu.operand);
                    cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
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
                    cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
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
                },
                cpu =>
                {
                    cpu.Write(cpu.address, operation(cpu, cpu.operand));
                    cpu.TraceInstruction(operation.Method.Name, cpu.address, cpu.operand);
                }
            };
        }

        public static void AbsoluteAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation) =>
            AbsoluteAddressing(cpu, Operation(operation));
        public static void AbsoluteAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation) =>
            AbsoluteAddressing(cpu, Operation(operation));
        public static void AbsoluteAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation) =>
            AbsoluteAddressing(cpu, Operation(operation));

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

        private static void AbsoluteAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations)
        {
            cpu.Enqueue(ReadPCToAddress);
            cpu.Enqueue(ReadPCToAddressHigh);
            //cpu.Enqueue(c => c.operand = c.Read(c.address));
            //cpu.Enqueue(c => c.Write(c.address, c.operand));
            //cpu.Enqueue(c=>c.Write(c.address, ))
            cpu.Enqueue(operations);
            //// 2    PC     R  fetch low byte of address, increment PC
            //address.Ptr = Read(this.regs.PC++);
            //yield return cycleTrace;

            //// 3    PC     R  fetch high byte of address, increment PC
            //address.High = Read(this.regs.PC++);
            //yield return cycleTrace;

            //// 4  address  R  read from effective address
            //var operand = Read(address);
            //yield return cycleTrace;

            //// 5  address  W  write the value back to effective address,
            ////                and do the operation on it
            //Write(address, operand);
            //var result = microcode(operand);
            //yield return cycleTrace;

            //// 6  address  W  write the new value to effective address
            //Write(address, result);
            //yield return cycleTrace;
            //TraceInstruction(microcode.Method.Name, address);

            //void Work(Ricoh2AFunctional cpu)
            //{
            //    ReadPCToAddressHigh(cpu);
            //    cpu.regs.PC = cpu.address;
            //    cpu.TraceInstruction(operation.Method.Name, cpu.address);
            //}

            //cpu.workQueue.Enqueue(ReadPCToAddress);
            //cpu.workQueue.Enqueue(operations);
        }

        private static void AbsoluteIndirectAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional> operation)
        {
            cpu.Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c => c.pointer.High = c.Read(c.regs.PC++));
            cpu.Enqueue(c => c.address.Ptr = c.Read(c.pointer));
            cpu.Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                operation(c);
                c.TraceInstruction(operation.Method.Name, c.address);
            });
            ////      2     PC      R  fetch pointer address low, increment PC
            //pointer.Ptr = Read(this.regs.PC++);
            //yield return cycleTrace;

            ////      3     PC      R  fetch pointer address high, increment PC
            //pointer.High = Read(this.regs.PC++);
            //yield return cycleTrace;

            ////      4   pointer   R  fetch low address to latch
            //address.Ptr = Read(pointer);
            //yield return cycleTrace;

            ////      5  pointer+1* R  fetch PCH, copy latch to PCL
            //pointer.Low++;
            //address.High = Read(pointer);
            //microcode(address);
            //yield return cycleTrace;
            //TraceInstruction(microcode.Method.Name, pointer);

            ////Note: * The PCH will always be fetched from the same page
            ////        than PCL, i.e. page boundary crossing is not handled.
        }

        private static void AbsoluteIndexedAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation, byte index)
        {
            cpu.Enqueue(c => c.address.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c =>
            {
                c.address.High = c.Read(c.regs.PC++);
                c.pointer.Ptr = (ushort)(c.address.Ptr + index);
                c.address.Low = c.pointer.Low;
            });
            cpu.Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                if (c.address.High == c.pointer.High)
                {
                    operation(c, c.operand);
                }
                else
                {
                    c.address.High = c.pointer.High;
                    c.Enqueue(Operation(operation));
                }
            });
        }

        private static void AbsoluteIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation, byte index) =>
            AbsoluteIndexedAddressing(cpu, Operation(operation), index);
        private static void AbsoluteIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation, byte index) =>
            AbsoluteIndexedAddressing(cpu, Operation(operation), index);

        private static void AbsoluteIndexedAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations, byte index)
        {
            cpu.Enqueue(c => c.address.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c =>
            {
                c.address.High = c.Read(c.regs.PC++);
                c.pointer.Ptr = (ushort)(c.address.Ptr + index);
                c.address.Low = c.pointer.Low;
            });
            cpu.Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                c.address.High = c.pointer.High;
            });
            cpu.Enqueue(operations);
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

        private static void ImmediateAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation)
        {
            cpu.Enqueue(c =>
            {
                c.operand = c.Read(c.regs.PC++);
                operation(c, c.operand);
                c.TraceInstruction(operation.Method.Name, c.operand);
            });
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

        public static void IndexedIndirectAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation) =>
            IndexedIndirectAddressing(cpu, Operation(operation));
        public static void IndexedIndirectAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation) =>
            IndexedIndirectAddressing(cpu, Operation(operation));
        public static void IndexedIndirectAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation) =>
            IndexedIndirectAddressing(cpu, Operation(operation));

        public static void IndexedIndirectAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations)
        {
            cpu.Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c =>
            {
                c.Read(c.pointer);
                c.pointer.Low += c.regs.X;
            });
            cpu.Enqueue(c => c.address.Ptr = c.Read(c.pointer));
            cpu.Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
            });
            cpu.Enqueue(operations);
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

        public static void IndirectIndexedAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation)
        {
            cpu.Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c =>
            {
                c.address.Ptr = c.Read(c.pointer);
            });
            cpu.Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                c.pointer.Ptr = (ushort)(c.address.Ptr + c.regs.Y);
                c.address.Low = c.pointer.Low;
            });
            cpu.Enqueue(c =>
            {
                c.operand = c.Read(c.address);
                if (c.address.High == c.pointer.High)
                {
                    operation(c, c.operand);
                }
                else
                {
                    c.address.High = c.pointer.High;
                    cpu.Enqueue(Operation(operation));
                }
            });
        }

        public static void IndirectIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation) =>
            IndirectIndexedAddressing(cpu, Operation(operation));

        public static void IndirectIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation) =>
            IndirectIndexedAddressing(cpu, Operation(operation));

        private static void IndirectIndexedAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations)
        {
            cpu.Enqueue(c => c.pointer.Ptr = c.Read(c.regs.PC++));
            cpu.Enqueue(c =>
            {
                c.address.Ptr = c.Read(c.pointer);
            });
            cpu.Enqueue(c =>
            {
                c.pointer.Low++;
                c.address.High = c.Read(c.pointer);
                c.pointer.Ptr = (ushort)(c.address.Ptr + c.regs.Y);
                c.address.Low = c.pointer.Low;
            });
            cpu.Enqueue(c =>
            {
                c.Read(c.address);
                c.address.High = c.pointer.High;
            });
            cpu.Enqueue(operations);
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

        private static void RelativeAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, bool> operation)
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
                    cpu.TraceInstruction(operation.Method.Name, cpu.address);
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
                    cpu.TraceInstruction(operation.Method.Name, cpu.address);
                }
            }

            void FixHigh(Ricoh2AFunctional cpu)
            {
                cpu.Read(cpu.regs.PC);
                cpu.regs.PC.High = cpu.address.High;
                cpu.TraceInstruction(operation.Method.Name, cpu.address);
            }

            //cpu.Enqueue(ReadOperand);
            cpu.Enqueue(Conditional);

            ////2     PC      R  fetch operand, increment PC
            //var operand = Read(this.regs.PC++);
            //var jumpAddress = this.regs.PC + (sbyte)operand;
            ////yield return cycleTrace;

            //if (microcode())
            //{
            //    //3     PC      R  Fetch opcode of next instruction,
            //    //                 If branch is taken, add operand to PCL.
            //    //                 Otherwise increment PC.
            //    Read(this.regs.PC);
            //    this.regs.PC.Low = jumpAddress.Low;
            //    yield return cycleTrace;

            //    if (this.regs.PC.High != jumpAddress.High)
            //    {
            //        //4+    PC*     R  Fetch opcode of next instruction.
            //        //                 Fix PCH. If it did not change, increment PC.
            //        Read(this.regs.PC);
            //        this.regs.PC.High = jumpAddress.High;
            //        yield return cycleTrace;
            //    }
            //}

            //yield return cycleTrace;
        }

        private static void ZeroPageAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation) =>
            ZeroPageAddressing(cpu, Operation(operation));
        private static void ZeroPageAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation) =>
            ZeroPageAddressing(cpu, Operation(operation));
        private static void ZeroPageAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation) =>
            ZeroPageAddressing(cpu, Operation(operation));

        private static void ZeroPageAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations)
        {
            cpu.Enqueue(ReadPCToAddress);
            cpu.Enqueue(operations);
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

        private static void ZeroPageIndexedAddressing(Ricoh2AFunctional cpu, Action<Ricoh2AFunctional, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(cpu, Operation(operation), index);
        private static void ZeroPageIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(cpu, Operation(operation), index);
        private static void ZeroPageIndexedAddressing(Ricoh2AFunctional cpu, Func<Ricoh2AFunctional, byte, byte> operation, byte index) =>
            ZeroPageIndexedAddressing(cpu, Operation(operation), index);

        private static void ZeroPageIndexedAddressing(Ricoh2AFunctional cpu, IEnumerable<Action<Ricoh2AFunctional>> operations, byte index)
        {
            cpu.Enqueue(ReadPCToAddress);
            cpu.Enqueue(c => c.address.Low += index);
            cpu.Enqueue(operations);
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
        }

        public Trace DoCycle()
        {
            if (workQueue.Count == 0)
            {
                Enqueue(QueueOpCode);
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
