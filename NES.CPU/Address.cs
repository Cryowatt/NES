using System;
using System.Runtime.InteropServices;

namespace NES.CPU
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Address : IEquatable<Address>, IComparable<Address>
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

        public static Address operator +(Address address, byte offset) => (Address)(address.Ptr + offset);
        public static Address operator +(Address address, ushort offset) => (Address)(address.Ptr + offset);

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
        public static implicit operator ushort(Address address) => address.Ptr;

        public override string ToString()
        {
            return string.Format("0x{0:X4}", this.Ptr);
        }

        public int CompareTo(Address other)
        {
            return (int)this.Ptr - (int)other.Ptr;
        }
    }
}
