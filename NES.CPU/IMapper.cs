using System;

namespace NES.CPU
{
    public interface IMapper : IBusDevice, IPPUBusDevice
    {
        ReadOnlyMemory<byte> PatternTable { get; }
    }
}