namespace NES.CPU
{
    public interface IPPUBusDevice
    {
        byte Read(Address address);

        void Write(Address address, byte value);
    }
}
