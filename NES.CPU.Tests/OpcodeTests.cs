using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NES.CPU.Tests
{
    public class OpcodeTests
    {
        public class MicrocodeTestInput
        {
            public MicrocodeTestInput(byte operand)
            {
                this.Operand = operand;
            }

            public byte Operand { get; }
            public byte ExpectedResult { get; private set; }
            public CpuRegisters InitialState = CpuRegisters.Empty;
            public CpuRegisters ExpectedState = CpuRegisters.Empty;

            public MicrocodeTestInput WithA(byte value)
            {
                this.InitialState.A = value;
                return this;
            }

            public MicrocodeTestInput WithFlag(StatusFlags flags)
            {
                this.InitialState.P |= flags;
                return this;
            }

            public MicrocodeTestInput Expects(byte expectedResult, StatusFlags flags = StatusFlags.None)
            {
                this.ExpectedResult = expectedResult;
                this.ExpectedState.P = flags | StatusFlags.Default;
                return this;
            }

            public MicrocodeTestInput Expects(StatusFlags flags)
            {
                this.ExpectedState.P = flags | StatusFlags.Default;
                return this;
            }

            public MicrocodeTestInput ExpectsA(byte value)
            {
                this.ExpectedState.A = value;
                return this;
            }
        }

        private static MicrocodeTestInput Operand(byte operand) => new MicrocodeTestInput(operand);

        [MemberData(nameof(ADCTestCases))]
        [Theory]
        public void ADC(MicrocodeTestInput input) =>
            OpcodeTest((cpu, operand) => cpu.ADC(operand), input);

        public static IEnumerable<object[]> ADCTestCases => new List<MicrocodeTestInput>
        {
            Operand(1).WithA(1).ExpectsA(2), // 1 + 1 = 2
            Operand(0xff).WithA(1).ExpectsA(0).Expects(StatusFlags.Carry | StatusFlags.Zero), // 1 + -1 = 0
            Operand(0x01).WithA(0x7f).ExpectsA(0x80).Expects(StatusFlags.Overflow | StatusFlags.Negative), // 127 + 1 = 128
            Operand(0x80).WithA(0xff).ExpectsA(0x7f).Expects(StatusFlags.Carry | StatusFlags.Overflow), // -128 + -1 = -129
            Operand(0x40).WithA(0x3f).WithFlag(StatusFlags.Carry).ExpectsA(0x80).Expects(StatusFlags.Overflow | StatusFlags.Negative), // -128 + -1 = -129
        }.Select(o => new object[] { o });

        [MemberData(nameof(ANDTestCases))]
        [Theory]
        public void AND(MicrocodeTestInput input) =>
            OpcodeTest((cpu, operand) => cpu.AND(operand), input);

        public static IEnumerable<object[]> ANDTestCases => new List<MicrocodeTestInput>
        {
            Operand(0xff).WithA(0).ExpectsA(0).Expects(StatusFlags.Zero),
            Operand(0xff).WithA(0x80).ExpectsA(0x80).Expects(StatusFlags.Negative),
            Operand(0xff).WithA(0xff).ExpectsA(0xff).Expects(StatusFlags.Negative),
            Operand(0x55).WithA(0xaa).ExpectsA(0).Expects(StatusFlags.Zero),
            Operand(0x1).WithA(0x1).ExpectsA(0x1),
            Operand(0x3).WithA(0x1).ExpectsA(0x1),
            Operand(0x1).WithA(0x3).ExpectsA(0x1),
        }.Select(o => new object[] { o });

        [MemberData(nameof(ASLTestCases))]
        [Theory]
        public void ASL(MicrocodeTestInput input) =>
            OpcodeTest((cpu, operand) => cpu.ASL(operand), input);

        public static IEnumerable<object[]> ASLTestCases => new List<MicrocodeTestInput>
        {
            Operand(0x0).Expects(0x0, StatusFlags.Zero),
            Operand(0x1).Expects(0x2),
            Operand(0x40).Expects(0x80, StatusFlags.Negative),
            Operand(0x80).Expects(0x00, StatusFlags.Carry | StatusFlags.Zero),
        }.Select(o => new object[] { o });

        private void OpcodeTest(Action<Ricoh2A, byte> opcode, MicrocodeTestInput input)
        {
            var ram = new Ram(new AddressRange(0x0000, 0xffff), 0xffff);
            var rambus = new Bus(ram, ram, ram, ram, ram, ram, ram, ram);
            var cpu = new Ricoh2A(rambus, input.InitialState);
            opcode(cpu, input.Operand);
            Assert.Equal(input.ExpectedState, cpu.Registers);
        }

        private void OpcodeTest(Func<Ricoh2A, byte, byte> opcode, MicrocodeTestInput input)
        {
            var ram = new Ram(new AddressRange(0x0000, 0xffff), 0xffff);
            var rambus = new Bus(ram, ram, ram, ram, ram, ram, ram, ram);
            var cpu = new Ricoh2A(rambus, input.InitialState);
            var result = opcode(cpu, input.Operand);
            Assert.Equal(input.ExpectedResult, result);
            Assert.Equal(input.ExpectedState, cpu.Registers);
        }
    }
}
