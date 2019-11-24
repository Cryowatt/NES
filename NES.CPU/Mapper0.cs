namespace NES.CPU
{
    public class Mapper0 : IMapper
    {
        private readonly RomImage image;
        public Mapper0(RomImage image)
        {
            this.image = image;
        }

        public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

        public byte Read(Address address)
        {
            //CPU $6000-$7FFF: Family Basic only: PRG RAM, mirrored as necessary to fill entire 8 KiB window, write protectable with an external switch
            //CPU $8000-$BFFF: First 16 KB of ROM.
            //CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
            return image.ProgramRomData.Span[address - 0x8000];
        }

        public void Write(Address address, byte value)
        {
            throw new System.NotImplementedException();
        }
    }
}