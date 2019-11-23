using System;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    public struct AddressRange
    {
        public readonly Address StartAddress;
        public readonly Address EndAddress;

        public AddressRange(Address address, ushort length)
        {
            this.StartAddress = address;
            this.EndAddress = address + length;
        }

        public bool Contains(Address address) => (this.StartAddress <= address) && (address <= this.EndAddress);

        public override string ToString()
        {
            return string.Format("0x{0:X4}::0x{1:X4}", this.StartAddress, this.EndAddress);
        }
    }
}
