using System.Collections.Generic;
using System.Linq;

namespace NES.CPU
{
    public class Bus : IBus
    {
        private readonly IBusDevice[] devices;

        public Bus(params IBusDevice[] devices) : this(devices.AsEnumerable())
        {
        }

        public Bus(IEnumerable<IBusDevice> devices)
        {
            this.devices = devices.OrderBy(o => o.AddressRange.StartAddress).ToList().ToArray();
        }

        private byte lastWrite;

        public byte Read(Address ptr)
        {
            var result = this.devices.FirstOrDefault(o => o.AddressRange.Contains(ptr))?.Read(ptr) ?? this.lastWrite;
            return result;
        }

        public void Write(Address ptr, byte value)
        {
            this.devices.FirstOrDefault(o => o.AddressRange.Contains(ptr))?.Write(ptr, value);
            this.lastWrite = value;
        }
    }
}
