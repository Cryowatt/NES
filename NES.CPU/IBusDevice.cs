namespace NES.CPU
{
    public interface IBusDevice
    {
        byte Read(Address address);

        void Write(Address address, byte value);
    }
}
