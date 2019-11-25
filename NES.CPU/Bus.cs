using System.Collections.Generic;
using System.Linq;

namespace NES.CPU
{
    public class NesBus : Bus
    {
        //$0000-$07FF	$0800	2KB internal RAM
        //$0800-$0FFF	$0800	Mirrors of $0000-$07FF
        //$1000-$17FF	$0800
        //$1800-$1FFF	$0800
        //$2000-$2007	$0008	NES PPU registers
        //$2008-$3FFF	$1FF8	Mirrors of $2000-2007 (repeats every 8 bytes)
        //$4000-$4017	$0018	NES APU and I/O registers
        //$4018-$401F	$0008	APU and I/O functionality that is normally disabled. See CPU Test Mode.
        //$4020-$FFFF	$BFE0	Cartridge space: PRG ROM, PRG RAM, and mapper registers (See Note)
        public NesBus(IMapper mapper)
            : base(
                new Ram(new AddressRange(0x0000, 0x1fff), 0x0800),
                mapper)
        { }
    }

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
