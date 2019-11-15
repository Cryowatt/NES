using System;
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
            this.devices = devices.OrderBy(o => o.AddressMask).ToArray();
        }

        private byte lastWrite;

        public void Write(Address ptr, byte value)
        {
            this.devices.First(o => o.AddressMask.Contains(ptr)).Write(ptr, value);
            this.lastWrite = value;
        }
    }
}
