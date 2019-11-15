using System;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    public struct AddressMask
    {
        public readonly Address Address;
        public readonly ushort Mask;

        public AddressMask(Address address, ushort mask)
        {
            this.Address = address;
            this.Mask = mask;
        }

        public bool Contains(Address address)
        {
            return (address.Ptr & Mask) == this.Address;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X4}", this.Mask);
        }
    }
}
