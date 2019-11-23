namespace NES.CPU
{
    public interface IBusDevice
    {
        AddressRange AddressRange { get; }

        byte Read(Address address);

        void Write(Address address, byte value);
    }
}
