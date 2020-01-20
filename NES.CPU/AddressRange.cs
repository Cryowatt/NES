using System;

namespace NES.CPU
{
    public struct AddressRange :IComparable<Address>
    {
        public readonly ushort StartAddress;
        public readonly ushort EndAddress;

        public AddressRange(Address startAddress, Address endAddress)
        {
            this.StartAddress = startAddress.Ptr;
            this.EndAddress = endAddress.Ptr;
        }

        public AddressRange(ushort startAddress, ushort endAddress)
        {
            this.StartAddress = startAddress;
            this.EndAddress = endAddress;
        }

        public int CompareTo(Address other)
        {
            if (this.EndAddress < other.Ptr)
            {
                return -1;
            }
            else if (this.StartAddress > other.Ptr)
            {
                return 1;
            }            
            else
            {
                return 0;
            }
        }

        public bool Contains(Address address) => (this.StartAddress <= address.Ptr) && (address.Ptr <= this.EndAddress);

        public override string ToString()
        {
            return string.Format("${0:X4}::${1:X4}", this.StartAddress, this.EndAddress);
        }
    }
}
