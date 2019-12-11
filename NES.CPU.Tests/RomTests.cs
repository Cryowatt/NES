﻿using NES.CPU.Mappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;

namespace NES.CPU.Tests
{
    public class RomTests
    {
        public class ExecutionState
        {
            public ExecutionState() { }

            public ExecutionState(IRicoh2A cpu)
            {
                this.Registers = cpu.Registers;
                this.CycleCount = cpu.CycleCount;
            }

            public CpuRegisters Registers { get; set; }
            public long CycleCount { get; set; }
        }

        public static IEnumerable<(ExecutionState Expected, ExecutionState Actual)> NesTestInstructions()
        {
            var neslogParser = new Regex(@"(?<OpAddress>[0-9A-F]{4})\s+(?<OpCode>[0-9A-F]{2})\s+(?<Operand>(?:[0-9A-F]{2}\s+)*?)(?<Op>\*?[A-Z]{3})\s+(?<OperandText>.*?)\s+A:(?<A>[0-9A-F]{2})\s+X:(?<X>[0-9A-F]{2})\s+Y:(?<Y>[0-9A-F]{2})\s+P:(?<P>[0-9A-F]{2})\s+SP:(?<SP>[0-9A-F]{2})\s+PPU:(?<PPU>\s*?\d+,\s*?\d+)\s+CYC:(?<CYC>\d+)");
            var truthSource = File.ReadLines("TestRoms/nestest.log");
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/nestest.nes")))
            {
                var state = new Queue<ExecutionState>();
                var romFile = RomImage.From(reader);
                var mapper = new Mapper0(romFile);
                var bus = new NesBus(mapper);
                bus.Write(0x6001, 0xc0);
                var cpu = new Ricoh2A(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
                bool instructionTriggered = false;
                var process = cpu.Process().GetEnumerator();
                // Bootstrap
                Assert.True(process.MoveNext());
                Assert.True(process.MoveNext());
                state.Enqueue(new ExecutionState(cpu));
                ExecutionState expected = new ExecutionState();
                var trunumerator = truthSource.GetEnumerator();

                void InstructionTrace(InstructionTrace trace)
                {
                    var parsedLog = neslogParser.Match(trunumerator.Current);
                    try
                    {
                        expected.CycleCount = long.Parse(parsedLog.Groups["CYC"].Value);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(trunumerator.Current + " WTF: " + parsedLog.Groups["CYC"].Value, e);
                    }
                    expected.Registers = new CpuRegisters
                    {
                        A = byte.Parse(parsedLog.Groups["A"].Value, NumberStyles.HexNumber),
                        X = byte.Parse(parsedLog.Groups["X"].Value, NumberStyles.HexNumber),
                        Y = byte.Parse(parsedLog.Groups["Y"].Value, NumberStyles.HexNumber),
                        PC = ushort.Parse(parsedLog.Groups["OpAddress"].Value, NumberStyles.HexNumber),
                        S = byte.Parse(parsedLog.Groups["SP"].Value, NumberStyles.HexNumber),
                        P = (StatusFlags)byte.Parse(parsedLog.Groups["P"].Value, NumberStyles.HexNumber),
                    };

                    instructionTriggered = true;
                    state.Enqueue(new ExecutionState(cpu));
                }
                cpu.InstructionTrace += InstructionTrace;

                while (trunumerator.MoveNext())
                {
                    while (!instructionTriggered)
                    {
                        try
                        {
                            Assert.True(process.MoveNext());
                        }
                        catch (Exception)
                        {
                            // Couldn't finish exection for whatever reason
                            break;
                        }
                    }
                    yield return (expected, state.Dequeue());
                    instructionTriggered = false;
                }
            }
        }
        public static IEnumerable<(ExecutionState Expected, ExecutionState Actual)> NesTestInstructionsForFuncCpu()
        {
            var neslogParser = new Regex(@"(?<OpAddress>[0-9A-F]{4})\s+(?<OpCode>[0-9A-F]{2})\s+(?<Operand>(?:[0-9A-F]{2}\s+)*?)(?<Op>\*?[A-Z]{3})\s+(?<OperandText>.*?)\s+A:(?<A>[0-9A-F]{2})\s+X:(?<X>[0-9A-F]{2})\s+Y:(?<Y>[0-9A-F]{2})\s+P:(?<P>[0-9A-F]{2})\s+SP:(?<SP>[0-9A-F]{2})\s+PPU:(?<PPU>\s*?\d+,\s*?\d+)\s+CYC:(?<CYC>\d+)");
            var truthSource = File.ReadLines("TestRoms/nestest.log");
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/nestest.nes")))
            {
                var state = new Queue<ExecutionState>();
                var romFile = RomImage.From(reader);
                var mapper = new Mapper0(romFile);
                var bus = new NesBus(mapper);
                bus.Write(0x6001, 0xc0);
                var cpu = new Ricoh2AFunctional(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
                bool instructionTriggered = false;
                cpu.Reset();
                // Bootstrap
                cpu.DoCycle();
                cpu.DoCycle();
                state.Enqueue(new ExecutionState(cpu));
                ExecutionState expected = new ExecutionState();
                var trunumerator = truthSource.GetEnumerator();

                void InstructionTrace(InstructionTrace trace)
                {
                    instructionTriggered = true;
                    state.Enqueue(new ExecutionState(cpu));
                }
                cpu.InstructionTrace += InstructionTrace;

                while (trunumerator.MoveNext())
                {
                    var parsedLog = neslogParser.Match(trunumerator.Current);
                    try
                    {
                        expected.CycleCount = long.Parse(parsedLog.Groups["CYC"].Value);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(trunumerator.Current + " WTF: " + parsedLog.Groups["CYC"].Value, e);
                    }
                    expected.Registers = new CpuRegisters
                    {
                        A = byte.Parse(parsedLog.Groups["A"].Value, NumberStyles.HexNumber),
                        X = byte.Parse(parsedLog.Groups["X"].Value, NumberStyles.HexNumber),
                        Y = byte.Parse(parsedLog.Groups["Y"].Value, NumberStyles.HexNumber),
                        PC = ushort.Parse(parsedLog.Groups["OpAddress"].Value, NumberStyles.HexNumber),
                        S = byte.Parse(parsedLog.Groups["SP"].Value, NumberStyles.HexNumber),
                        P = (StatusFlags)byte.Parse(parsedLog.Groups["P"].Value, NumberStyles.HexNumber),
                    };

                    while (!instructionTriggered)
                    {
                        cpu.DoCycle();
                    }
                    yield return (expected, state.Dequeue());
                    instructionTriggered = false;
                }
            }
        }

        [Fact]
        public void NesTestFunctional()
        {
            int instructionCount = 0;
            foreach (var instruction in NesTestInstructionsForFuncCpu())
            {
                instructionCount++;
                instruction.Actual.CycleCount.Should().Be(instruction.Expected.CycleCount, "cycle failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.A.Should().Be(instruction.Expected.Registers.A, "A failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.X.Should().Be(instruction.Expected.Registers.X, "X failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.Y.Should().Be(instruction.Expected.Registers.Y, "Y failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.PC.Should().Be(instruction.Expected.Registers.PC, "PC failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.S.Should().Be(instruction.Expected.Registers.S, "S failed at instruction {0}", instructionCount);
                ((byte)instruction.Actual.Registers.P).Should().Be((byte)instruction.Expected.Registers.P, "P failed at instruction {0}", instructionCount);
            }
        }

        [Fact]
        public void NesTestRealLol()
        {
            int instructionCount = 0;
            foreach (var instruction in NesTestInstructions())
            {
                instructionCount++;
                instruction.Actual.CycleCount.Should().Be(instruction.Expected.CycleCount, "cycle failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.A.Should().Be(instruction.Expected.Registers.A, "A failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.X.Should().Be(instruction.Expected.Registers.X, "X failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.Y.Should().Be(instruction.Expected.Registers.Y, "Y failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.PC.Should().Be(instruction.Expected.Registers.PC, "PC failed at instruction {0}", instructionCount);
                instruction.Actual.Registers.S.Should().Be(instruction.Expected.Registers.S, "S failed at instruction {0}", instructionCount);
                ((byte)instruction.Actual.Registers.P).Should().Be((byte)instruction.Expected.Registers.P, "P failed at instruction {0}", instructionCount);
            }
        }

        //[Fact]
        //public void NesTest()
        //{
        //    var neslogParser = new Regex(@"(?<OpAddress>[0-9A-F]{4})\s+(?<OpCode>[0-9A-F]{2})\s+(?<Operand>(?:[0-9A-F]{2}\s+)*?)(?<Op>[A-Z]{3})\s+(?<OperandText>.*?)\s+A:(?<A>[0-9A-F]{2})\s+X:(?<X>[0-9A-F]{2})\s+Y:(?<Y>[0-9A-F]{2})\s+P:(?<P>[0-9A-F]{2})\s+SP:(?<SP>[0-9A-F]{2})\s+PPU:(?<PPU>\s*?\d+,\s*?\d+)\s+CYC:(?<CYC>\d+)");
        //    var truthSource = File.ReadLines("TestRoms/nestest.log");
        //    using (var reader = new BinaryReader(File.OpenRead("TestRoms/nestest.nes")))
        //    {
        //        var romFile = RomImage.From(reader);
        //        var mapper = new Mapper0(romFile);
        //        var bus = new NesBus(mapper);
        //        bus.Write(0x6001, 0xc0);
        //        var cpu = new Ricoh2A(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
        //        bool instructionTriggered = false;
        //        var process = cpu.Process().GetEnumerator();
        //        // Bootstrap
        //        Assert.True(process.MoveNext());
        //        Assert.True(process.MoveNext());
        //        var initState = cpu.Registers;
        //        var trunumerator = truthSource.GetEnumerator();
        //        void InstructionTrace(InstructionTrace trace)
        //        {
        //            var parsedLog = neslogParser.Match(trunumerator.Current);
        //            var expected = new CpuRegisters
        //            {
        //                A = byte.Parse(parsedLog.Groups["A"].Value, NumberStyles.HexNumber),
        //                PC = ushort.Parse(parsedLog.Groups["OpAddress"].Value, NumberStyles.HexNumber),
        //                S = byte.Parse(parsedLog.Groups["SP"].Value, NumberStyles.HexNumber),
        //                P = (StatusFlags)byte.Parse(parsedLog.Groups["P"].Value, NumberStyles.HexNumber),
        //            };

        //            Assert.Equal(expected, initState);
        //            instructionTriggered = true;
        //            initState = cpu.Registers;
        //        }
        //        cpu.InstructionTrace += InstructionTrace;

        //        while (trunumerator.MoveNext())
        //        {
        //            while (!instructionTriggered)
        //            {
        //                Assert.True(process.MoveNext());
        //            }
        //            instructionTriggered = false;
        //        }
        //    }
        //}

        //[Fact]
        //public void Basics()
        //{
        //    using (var reader = new BinaryReader(File.OpenRead("TestRoms/01-basics.nes")))
        //    {
        //        var romFile = RomImage.From(reader);
        //        var bus = new NesBus(new Mapper0(romFile));
        //        var cpu = new Ricoh2A(bus);
        //        int howFuckingFarDidIEvenGet = 0;
        //        var process = cpu.Process();
        //        var bootStrap = process.TakeWhile(o => bus.Read(0x6000) != 0x80).Count();

        //        foreach (var cycle in process.Take(1000000))
        //        {
        //            howFuckingFarDidIEvenGet++;
        //            Assert.Equal(0x80, bus.Read(0x6000));
        //        }
        //    }
        //}
    }
}
