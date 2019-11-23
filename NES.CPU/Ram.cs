namespace NES.CPU
{
    public class Ram : IBusDevice
    {
        private byte[] memory;

        public Ram(AddressRange addressRange, int size)
        {
            this.AddressRange = addressRange;
            this.memory = new byte[size];
        }

        public AddressRange AddressRange { get; private set; }

        public byte Read(Address address)
        {
            return this.memory[address % this.memory.Length];
        }

        public void Write(Address address, byte value)
        {
            this.memory[address % this.memory.Length] = value;
        }
    }
}
