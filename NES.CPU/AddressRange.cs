using System;

namespace NES.CPU
{
    public struct AddressRange :IComparable<Address>
    {
        public readonly Address StartAddress;
        public readonly Address EndAddress;

        public AddressRange(Address startAddress, Address endAddress)
        {
            this.StartAddress = startAddress;
            this.EndAddress = endAddress;
        }

        public int CompareTo(Address other)
        {
            if (this.EndAddress.Ptr < other.Ptr)
            {
                return -1;
            }
            else if (this.StartAddress.Ptr > other.Ptr)
            {
                return 1;
            }            
            else
            {
                return 0;
            }
        }

        public bool Contains(Address address) => (this.StartAddress.Ptr <= address.Ptr) && (address.Ptr <= this.EndAddress.Ptr);

        public override string ToString()
        {
            return string.Format("${0:X4}::${1:X4}", this.StartAddress, this.EndAddress);
        }
    }
}
