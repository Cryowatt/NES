using System;
using System.Collections.Generic;
using System.Linq;

namespace NES
{
    public class Bus : IBus
    {
        // I'm making the assumption that each device uses the first three bits of the address as a chip-select. This means there are only 8 device slots in total (0b000, 0b111).
        private readonly IBusDevice[] devices;

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
        //{
        //    return t ptr.Ptr >> 5
        //    int scale = this.devices.Length >> 1;

        //    for (int i = scale; scale > 0; scale >>= 1)
        //    {
        //        //int i = scale;
        //        //scale >>= 1;
        //        int blerg = this.devices[i].AddressRange.CompareTo(ptr);
        //        switch (blerg)
        //        {
        //            case 0:
        //                return this.devices[i];
        //            case 1:
        //                i += scale;
        //                break;
        //            case -1:
        //                i -= scale;
        //                break;
        //        }
        //        //if (blerg == 0)
        //        //{
        //        //    return this.devices[i];
        //        //}
        //        //scale >>= 1;
        //        //i += blerg * scale;

        //        //for (int i = scale; this.devices[i].AddressRange.CompareTo(ptr) != 0; i++)
        //        //{
        //        //    if (this.devices[i].AddressRange.Contains(ptr))
        //        //    {
        //        //        return this.devices[i];
        //        //    }
        //        //}

        //        //return null;
        //    }

        //    throw new InvalidOperationException();
        //}

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
