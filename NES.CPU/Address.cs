using System;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Address : IEquatable<Address>
    {
        [FieldOffset(0)]
        public ushort Ptr;
        [FieldOffset(0)]
        public byte Low;
        [FieldOffset(1)]
        public byte High;

        public Address(ushort address)
        {
            this.Low = this.High = 0;
            this.Ptr = address;
        }

        public override bool Equals(object obj)
        {
            return obj is Address address && this.Equals(address);
        }

        public bool Equals(Address other)
        {
            return this.Ptr == other.Ptr;
        }

        public override int GetHashCode()
        {
            return this.Ptr;
        }

        public static Address operator ++(Address address)
        {
            address.Ptr++;
            return address;
        }

        public static bool operator ==(Address left, Address right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Address left, Address right)
        {
            return !(left == right);
        }

        public static implicit operator Address(ushort address) => new Address(address);

        public override string ToString()
        {
            return this.Ptr.ToString("X4");
        }
    }
}
