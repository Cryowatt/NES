namespace NES.CPU
{
    public interface IBus
    {
        void Write(Address ptr, byte value);
    }
}
