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

        public Memory<int> FrameBuffer => ppu.FrameBuffer;

        public event Action<Trace> OnCycle;

        public Platform(IMapper mapper)
        {
            this.bus = new NesBus(mapper);
            this.cpu = new Ricoh2AFunctional(this.bus);
            this.ppu = new PPU(mapper);
        }

        public Platform(IBus bus, IRicoh2A cpu, PPU ppu)
        {
            this.bus = bus;
            this.cpu = cpu;
            this.ppu = ppu;
        }

        public void Reset()
        {
            this.cpu.Reset();
        }

        public void Run()
        {
            for (long ticks = 0; ; ticks += 12)
            {
                DoCycle();
            }
        }

        public void DoCycle()
        {
            this.cpu.DoCycle();
            this.ppu.DoCycle();
            this.ppu.DoCycle();
            this.ppu.DoCycle();
        }
    }
}
