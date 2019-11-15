namespace NES.CPU
{
    public class Ram : IBusDevice
    {
        private byte[] memory;

        public Ram(AddressMask addressMask, int size)
        {
            this.AddressMask = addressMask;
            this.memory = new byte[size];
        }

        public AddressMask AddressMask { get; private set; }

        public byte Read(Address address)
        {
            var memoryAddress = address - AddressMask.Address;
            return this.memory[memoryAddress];
        }

        public void Write(Address address, byte value)
        {
            var memoryAddress = address - AddressMask.Address;
            this.memory[memoryAddress] = value;
        }
    }
}
