namespace NES.CPU
{
    public struct AddressRange
    {
        public readonly Address StartAddress;
        public readonly Address EndAddress;

        public AddressRange(Address startAddress, Address endAddress)
        {
            this.StartAddress = startAddress;
            this.EndAddress = endAddress;
        }

        public bool Contains(Address address) => (this.StartAddress <= address) && (address <= this.EndAddress);

        public override string ToString()
        {
            return string.Format("${0:X4}::${1:X4}", this.StartAddress, this.EndAddress);
        }
    }
}
