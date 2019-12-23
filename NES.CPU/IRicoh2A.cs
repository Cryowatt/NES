namespace NES.CPU
{
    public interface IRicoh2A
    {
        CpuRegisters Registers { get; }
        long CycleCount { get; }

        void Reset();
        Trace DoCycle();
    }
}
