using System;
using System.Collections.Generic;
using System.Text;

namespace NES.CPU
{
    public class Platform
    {
        private const long Crystal = 236_250_003 / 11; // Added three for rounding purposes
        private readonly IBus bus;
        private readonly IRicoh2A cpu;
        private readonly PPU ppu;

        public Platform(IMapper mapper)
        {
            this.bus = new NesBus(mapper);
            this.cpu = new Ricoh2AFunctional(this.bus);
            this.ppu = new PPU();
        }

        public void Reset()
        {
            this.cpu.Reset();
        }

        public void Run()
        {
            for (long ticks = 0; ; ticks++)
            {
                if (ticks % 12 == 0)
                {
                    this.cpu.DoCycle();
                    this.ppu.DoCycle();
                    this.ppu.DoCycle();
                    this.ppu.DoCycle();
                }
            }
        }
    }
}
