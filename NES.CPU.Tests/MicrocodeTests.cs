using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace NES.CPU.Tests
{
    public class MicrocodeTests
    {
        public class MicrocodeTestInput
        {
            public MicrocodeTestInput(byte operand)
            {
                this.Operand = operand;
            }

            public byte Operand { get; }
            public CpuRegisters InitialState = CpuRegisters.Empty;
            public CpuRegisters ExpectedState = CpuRegisters.Empty;

            public MicrocodeTestInput WithA(byte value)
            {
                this.InitialState.A = value;
                return this;
            }

            public MicrocodeTestInput Expects(StatusFlags flags)
            {
                this.ExpectedState.P = flags;
                return this;
            }

            public MicrocodeTestInput ExpectsA(byte value)
            {
                this.ExpectedState.A = value;
                return this;
            }
        }

        public static MicrocodeTestInput Operand(byte operand) => new MicrocodeTestInput(operand);

        public static IEnumerable<object[]> ADCTestCases => new List<MicrocodeTestInput>
        {
            Operand(1).WithA(1).ExpectsA(2), // 1 + 1 = 2
            Operand(0).Expects(StatusFlags.Zero), // Zero flag
            Operand(1).ExpectsA(1), // 0 + 1 = 1
            Operand(255).WithA(255).Expects(StatusFlags.Carry).ExpectsA(254),

//C	Carry Flag	Set if overflow in bit 7
//Z	Zero Flag	Set if A = 0
//I	Interrupt Disable	Not affected
//D	Decimal Mode Flag	Not affected
//B	Break Command	Not affected
//V	Overflow Flag	Set if sign bit is incorrect
//N	Negative Flag	Set if bit 7 set
        }.Select(o => new object[] { o });

        [MemberData(nameof(ADCTestCases))]
        [Theory]
        public void ADC(MicrocodeTestInput input)
        {
            var rambus = new Bus(new Ram(new AddressMask(0x0000, 0x0000), 0xffff));
            var cpu = new Ricoh2A(rambus, input.InitialState);
            cpu.ADC(input.Operand);
            Assert.Equal(input.ExpectedState, cpu.Registers);
        }
    }
}
