using System;
using System.Collections.Generic;
using System.Linq;

namespace NES.CPU
{
    public class Bus : IBus
    {
        // I'm making the assumption that each device uses the first three bits of the address as a chip-select. This means there are only 8 device slots in total (0b000, 0b111).
        protected readonly IBusDevice[] devices;

        public Bus(IBusDevice device0, IBusDevice device1, IBusDevice device2, IBusDevice device3, IBusDevice device4, IBusDevice device5, IBusDevice device6, IBusDevice device7)
        {
            devices = new IBusDevice[]
            {
                device0,
                device1,
                device2,
                device3,
                device4,
                device5,
                device6,
                device7,
            };
        }

        private byte lastWrite;

        public Address Address { get; set; }

        public byte Value
        {
            get => Read(Address);
            set => Write(Address, value);
        }

        public IBusDevice FindDevice(Address ptr) => this.devices[ptr.Ptr >> 13];

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
