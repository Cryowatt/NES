namespace NES.CPU
{
    public interface IBus
    {
        Address Address { get; set; }
        byte Value { get; set; }

        byte Read(Address address);
        void Write(Address address, byte value);
    }
}
