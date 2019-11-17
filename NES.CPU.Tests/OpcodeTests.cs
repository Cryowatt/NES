using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        public static MicrocodeTestInput Operand(byte operand) => new MicrocodeTestInput(operand);
        //CLC      ; 1 + 1 = 2, returns V = 0
        //LDA #$01
        //ADC #$01

        //CLC      ; 1 + -1 = 0, returns V = 0
        //LDA #$01
        //ADC #$FF

        //CLC      ; 127 + 1 = 128, returns V = 1
        //LDA #$7F
        //ADC #$01

        //CLC      ; -128 + -1 = -129, returns V = 1
        //LDA #$80
        //ADC #$FF

        //SEC      ; Note: SEC, not CLC
        //LDA #$3F ; 63 + 64 + 1 = 128, returns V = 1
        //ADC #$40

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
            //Operand(0).Expects(StatusFlags.Zero), // Zero flag
            //Operand(1).ExpectsA(1), // 0 + 1 = 1
            //Operand(255).WithA(255).Expects(StatusFlags.Carry).ExpectsA(254),
            //C	Carry Flag	Set if overflow in bit 7
            //Z	Zero Flag	Set if A = 0
            //I	Interrupt Disable	Not affected
            //D	Decimal Mode Flag	Not affected
            //B	Break Command	Not affected
            //V	Overflow Flag	Set if sign bit is incorrect
            //N	Negative Flag	Set if bit 7 set
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

        private void OpcodeTest(Action<Ricoh2A, byte> opcode, MicrocodeTestInput input)
        {
            var rambus = new Bus(new Ram(new AddressMask(0x0000, 0x0000), 0xffff));
            var cpu = new Ricoh2A(rambus, input.InitialState);
            opcode(cpu, input.Operand);
            Assert.Equal(input.ExpectedState, cpu.Registers);
        }
    }
}
