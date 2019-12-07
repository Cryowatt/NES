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
            this.devices = devices.OrderBy(o => o.AddressRange.StartAddress).ToList().ToArray();
        }

        private byte lastWrite;

        public Address Address { get; set; }

        public byte Value
        {
            get => Read(Address);
            set => Write(Address, value);
        }

        public IBusDevice FindDevice(Address ptr)
        {
            for (int i = 0; i < this.devices.Length; i++)
            {
                if (this.devices[i].AddressRange.Contains(ptr))
                {
                    return this.devices[i];
                }
            }

            return null;
        }

        public byte Read(Address ptr)
        {
            return FindDevice(ptr)?.Read(ptr) ?? this.lastWrite;
        }

        public void Write(Address ptr, byte value)
        {
            FindDevice(ptr)?.Write(ptr, value);
            this.lastWrite = value;
        }
    }
}
