namespace NES.CPU
{
    public interface IBusDevice
    {
        AddressMask AddressMask { get; }

        byte Read(Address address);

        void Write(Address address, byte value);
    }
}
