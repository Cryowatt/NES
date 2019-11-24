namespace NES.CPU
{
    public interface IBus
    {
        byte Read(Address address);
        void Write(Address address, byte value);
    }
}
