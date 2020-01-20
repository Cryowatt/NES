using NES.CPU.Mappers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NES.CPU.Tests
{
    [TestFixture]
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
            public string OpCode { get; internal set; }
        }

        public static IEnumerable<(ExecutionState Expected, ExecutionState Actual)> NesTestInstructionsForFuncCpu()
        {
            var neslogParser = new Regex(@"(?<OpAddress>[0-9A-F]{4})\s+(?<OpCode>[0-9A-F]{2})\s+(?<Operand>(?:[0-9A-F]{2}\s+)*?)(?<Op>\*?[A-Z]{3})\s+(?<OperandText>.*?)\s+A:(?<A>[0-9A-F]{2})\s+X:(?<X>[0-9A-F]{2})\s+Y:(?<Y>[0-9A-F]{2})\s+P:(?<P>[0-9A-F]{2})\s+SP:(?<SP>[0-9A-F]{2})\s+PPU:(?<PPU>\s*?\d+,\s*?\d+)\s+CYC:(?<CYC>\d+)");
            var truthSource = File.ReadLines("TestRoms/nestest.log");
            using (var reader = new BinaryReader(File.OpenRead("TestRoms/nestest.nes")))
            {
                var state = new Queue<ExecutionState>();
                var romFile = RomImage.From(reader);
                var mapper = Mapper.FromImage(romFile);
                var apu = new APU();
                var ppu = new PPU(mapper);
                var bus = new NesBus(mapper, ppu, apu);
                bus.Write(new Address(0x6001), 0xc0);
                var cpu = new Ricoh2AFunctional(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), new Address(0x6000));
                bool instructionTriggered = false;
                cpu.Reset();
                // Bootstrap
                cpu.DoCycle();
                cpu.DoCycle();
                state.Enqueue(new ExecutionState(cpu) { OpCode = "JMP" });
                ExecutionState expected = new ExecutionState();
                var trunumerator = truthSource.GetEnumerator();

                void InstructionTrace(InstructionTrace trace)
                {
                    instructionTriggered = true;
                    state.Enqueue(new ExecutionState(cpu)
                    {
                        OpCode = trace.Name
                    });
                }
                cpu.InstructionTrace += InstructionTrace;

                while (trunumerator.MoveNext())
                {
                    var parsedLog = neslogParser.Match(trunumerator.Current);
                    try
                    {
                        expected.OpCode = parsedLog.Groups["Op"].Value;
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
                        PC = new Address(ushort.Parse(parsedLog.Groups["OpAddress"].Value, NumberStyles.HexNumber)),
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

        //[Fact]
        //public void NesTestFunctional()
        //{
        //    int instructionCount = 0;
        //    foreach (var instruction in NesTestInstructionsForFuncCpu())
        //    {
        //        instructionCount++;
        //        //instruction.Actual.OpCode.Should().Be(instruction.Expected.OpCode, "OpCode failed at instruction {0}", instructionCount);
        //        instruction.Actual.CycleCount.Should().Be(instruction.Expected.CycleCount, "cycle failed at instruction {0}", instructionCount);
        //        instruction.Actual.Registers.A.Should().Be(instruction.Expected.Registers.A, "A failed at instruction {0}", instructionCount);
        //        instruction.Actual.Registers.X.Should().Be(instruction.Expected.Registers.X, "X failed at instruction {0}", instructionCount);
        //        instruction.Actual.Registers.Y.Should().Be(instruction.Expected.Registers.Y, "Y failed at instruction {0}", instructionCount);
        //        instruction.Actual.Registers.PC.Should().Be(instruction.Expected.Registers.PC, "PC failed at instruction {0}", instructionCount);
        //        instruction.Actual.Registers.S.Should().Be(instruction.Expected.Registers.S, "S failed at instruction {0}", instructionCount);
        //        ((byte)instruction.Actual.Registers.P).Should().Be((byte)instruction.Expected.Registers.P, "P failed at instruction {0}", instructionCount);
        //    }
        //}

        //[Test]
        //public void NesTest()
        //{
        //    using var reader = new BinaryReader(File.OpenRead("../../../../nes-test-roms/other/nestest.nes"));
        //    var mapper = Mapper.FromImage(RomImage.From(reader));
        //    var ppu = new PPU(mapper);
        //    var apu = new APU();
        //    var bus = new NesBus(mapper, ppu, apu, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
        //    bus.Write(0x6001, 0xc0);

        //    //var platform = new NesBusbus, cpu, new PPU(mapper));
        //    bus.DoCycle();
        //}

        [Theory]
        [TestCase("01-implied.nes", TestName = "Implied")]
        [TestCase("02-immediate.nes", TestName = "Immediate")]
        [TestCase("03-zero_page.nes", TestName = "ZeroPage")]
        [TestCase("04-zp_xy.nes", TestName = "ZeroPageIndexed")]
        [TestCase("05-absolute.nes", TestName = "Absolute")]
        [TestCase("06-abs_xy.nes", TestName = "AbsoluteIndexed")]
        [TestCase("07-ind_x.nes", TestName = "IndirectX")]
        [TestCase("08-ind_y.nes", TestName = "IndirectY")]
        [TestCase("09-branches.nes", TestName = "Branches")]
        [TestCase("10-stack.nes", TestName = "Stack")]
        [TestCase("11-jmp_jsr.nes", TestName = "JmpJsr")]
        [TestCase("12-rts.nes", TestName = "Rts")]
        [TestCase("13-rti.nes", TestName = "Rti")]
        [TestCase("14-brk.nes", TestName = "Brk")]
        [TestCase("15-special.nes", TestName = "Special")]
        public unsafe void InstructionTests(string path)
        {
            var romPath = Path.Combine(FindTestRomDirectory(), "instr_test-v3/rom_singles/", path);
            using var reader = new BinaryReader(File.OpenRead(romPath));
            var mapper = (Mapper0)Mapper.FromImage(RomImage.From(reader));
            var platform = new NesBus(mapper);
            platform.Reset();
            int result;
            // Test warmup, waiting for $6000 to go non-zero (usually 0x80 to indicate running state
            while (platform.Read(new Address(0x6000)) == 0)
            {
                platform.DoCycle();
            }

            while ((result = platform.Read(new Address(0x6000))) == 0x80)
            {
                platform.DoCycle();
            }

            // Checking for DEBO61
            Assert.AreEqual(0xDE, platform.Read(new Address(0x6001)));
            Assert.AreEqual(0xB0, platform.Read(new Address(0x6002)));
            Assert.AreEqual(0x61, platform.Read(new Address(0x6003)));
            
            using (var debugPin = mapper.Ram.Slice(4).Pin())
            {
                var errorMessage = Marshal.PtrToStringAnsi((IntPtr)debugPin.Pointer);
                Assert.AreEqual(0, result, errorMessage);
            }
        }

        /// <summary>
        /// The test directory changes depending on how the tests are run. This function will search for the correct location of the roms.
        /// </summary>
        private static string FindTestRomDirectory()
        {
            var testPath = TestContext.CurrentContext.TestDirectory;
            string romPath;

            while (!Directory.Exists(romPath = Path.Combine(testPath, "nes-test-roms")))
            {
                testPath = Path.GetDirectoryName(testPath);

                if (testPath == null)
                {
                    throw new Exception("Couldn't find the test roms");
                }
            }

            return romPath;
        }
    }
}
