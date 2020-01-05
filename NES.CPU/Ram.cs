using System;
using System.Buffers;

namespace NES.CPU
{
    public class Ram : IBusDevice
    {
        private Memory<byte> memory;
        private MemoryHandle memoryHandle;

        public Ram(AddressRange addressRange, int size)
        {
            this.AddressRange = addressRange;
            this.memory = new byte[size].AsMemory();
            this.memoryHandle = this.memory.Pin();
        }

        public ReadOnlyMemory<byte> Memory => this.memory;

        public AddressRange AddressRange { get; private set; }

        public unsafe byte Read(Address address)
        {
            return *((byte*)this.memoryHandle.Pointer + (address.Ptr % this.memory.Length));
        }

        public unsafe void Write(Address address, byte value)
        {
            *((byte*)this.memoryHandle.Pointer + (address.Ptr % this.memory.Length)) = value;
        }
    }
}
