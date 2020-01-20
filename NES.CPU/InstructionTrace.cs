using System;

namespace NES.CPU
{
    public class InstructionTrace
    {
        public Address PC { get; set; }
        public byte OpCode { get; set; }
        public string Name { get; set; }
        //public string Operand { get; set; }
        public byte Operand { get; set; }
        public Address Address { get; set; }
        private Func<string> messageBuilder;

        public InstructionTrace(Address pc, byte opcode, string name)
        {
            this.PC = pc;
            this.OpCode = opcode;
            this.Name = name;
            this.messageBuilder = () => string.Empty;
        }

        public InstructionTrace(Address pc, byte opcode, string name, Address address) : this(pc, opcode, name)
        {
            this.Address = address;
            this.Operand = 0;
            this.messageBuilder = () => $"#{this.Address}";
        }
        public InstructionTrace(Address pc, byte opcode, string name, byte operand) : this(pc, opcode, name)
        {
            this.Address = new Address();
            this.Operand = operand;
            this.messageBuilder = () => $"#${operand:X2}";
        }
        public InstructionTrace(Address pc, byte opcode, string name, Address address, byte operand) : this(pc, opcode, name)
        {
            this.Address = address;
            this.Operand = operand;
            this.messageBuilder = () => $"#{address} = #${operand:X2}";
        }

        public override string ToString()
        {
            return $"{PC} {OpCode:X2} {Name} {this.messageBuilder()}";
        }
    }
}