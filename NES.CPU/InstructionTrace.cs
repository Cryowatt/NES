namespace NES.CPU
{
    public class InstructionTrace
    {
        private Address pc;
        private byte opcode;
        private string name;
        private string operand;

        public InstructionTrace(Address pc, byte opcode, string name)
        {
            this.pc = pc;
            this.opcode = opcode;
            this.name = name;
        }
        public InstructionTrace(Address pc, byte opcode, string name, Address address) : this(pc, opcode, name)
        {
            this.operand = $"#{address}";
        }
        public InstructionTrace(Address pc, byte opcode, string name, byte operand) : this(pc, opcode, name)
        {
            this.operand = $"#${operand:X2}";
        }

        public override string ToString()
        {
            return $"{pc} {opcode:X2} {name} {operand}";
        }
    }
}