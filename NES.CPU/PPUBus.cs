using System;

namespace NES.CPU
{
    public class PPUBus : IBus
    {
        private Ram nametableRam = new Ram(new AddressRange(0x2000, 0x3F1F), 0x1000);
        private Ram paletteRam = new Ram(new AddressRange(0x3F00, 0x3FFF), 0x0020);
        private IMapper mapper;

        // $0000-$0FFF	$1000	Pattern table 0
        // $1000-$1FFF	$1000	Pattern table 1
        // $2000-$23FF	$0400	Nametable 0
        // $2400-$27FF	$0400	Nametable 1
        // $2800-$2BFF	$0400	Nametable 2
        // $2C00-$2FFF	$0400	Nametable 3
        // $3000-$3EFF	$0F00	Mirrors of $2000-$2EFF
        // $3F00-$3F1F	$0020	Palette RAM indexes
        // $3F20-$3FFF	$00E0	Mirrors of $3F00-$3F1F
        public PPUBus(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public ReadOnlyMemory<byte> Nametable => this.nametableRam.Memory;
        public ReadOnlyMemory<byte> PatternTable => this.mapper.PatternTable;

        // I'm making the assumption that each device uses the first three bits of the address as a chip-select. This means there are only 8 device slots in total (0b000, 0b111).
        protected readonly IBusDevice[] devices;

        private byte lastWrite;

        public Address Address { get; set; }

        public byte Value
        {
            get => Read(Address);
            set => Write(Address, value);
        }

        public IBusDevice FindDevice(Address ptr) => this.devices[ptr.Ptr >> 13];

        private byte ReadPalette(Address ptr)
        {
            // $3000-$3EFF	$0F00	Mirrors of $2000-$2EFF
            // $3F00-$3F1F	$0020	Palette RAM indexes
            // $3F20-$3FFF	$00E0	Mirrors of $3F00-$3F1F
            return ptr.High switch
            {
                0x3f => paletteRam.Read(ptr),
                _ => nametableRam.Read(ptr),
            };
        }

        public byte Read(Address ptr)
        {
            ptr.Ptr &= 0x3fff;
            var bank = ptr.Ptr >> 12;
            return bank switch
            {
                0x0 => ((IPPUBusDevice)this.mapper).Read(ptr),
                0x1 => ((IPPUBusDevice)this.mapper).Read(ptr),
                0x2 => this.nametableRam.Read(ptr),
                0x3 => this.ReadPalette(ptr),
                _ => throw new InvalidOperationException(),
            };
        }

        private void WritePalette(Address ptr, byte value)
        {
            // $3000-$3EFF	$0F00	Mirrors of $2000-$2EFF
            // $3F00-$3F1F	$0020	Palette RAM indexes
            // $3F20-$3FFF	$00E0	Mirrors of $3F00-$3F1F
            switch (ptr.High)
            {
                case 0x3f: paletteRam.Write(ptr, value); break;
                default: nametableRam.Write(ptr, value); break;
            };
        }

        public void Write(Address ptr, byte value)
        {
            ptr.Ptr &= 0x3fff;
            var bank = ptr.Ptr >> 12;
            switch (bank)
            {
                case 0x0: ((IPPUBusDevice)this.mapper).Write(ptr, value); break;
                case 0x1: ((IPPUBusDevice)this.mapper).Write(ptr, value); break;
                case 0x2: this.nametableRam.Write(ptr, value); break;
                case 0x3: this.WritePalette(ptr, value); break;
                default: throw new InvalidOperationException();
            };
        }
    }
}
