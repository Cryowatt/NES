using System;
using System.Diagnostics;
using System.Threading;

namespace NES.CPU
{
    public class NesBus : Bus
    {
        private const long Crystal = 236_250_003 / 11; // Added three for rounding purposes
        private readonly Ricoh2AFunctional cpu;
        private readonly APU apu;
        private readonly PPU ppu;
        private readonly Stopwatch timer = new Stopwatch();

        public NesBus(IMapper mapper, PPU ppu, APU apu)
            : base(
                new Ram(new AddressRange(0x0000, 0x1fff), 0x0800), // 0x0000
                ppu, //PPU 0x2000
                apu, //APU 0x4000
                mapper, // 0x6000
                mapper, // 0x8000
                mapper, // 0xA000
                mapper, // 0xC000
                mapper) // 0xE000
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
            this.ppu = ppu;
            this.apu = apu;
            this.cpu = new Ricoh2AFunctional(this);
            this.cpu.InstructionTrace += OnInstructionTrace;
        }

        public NesBus(IMapper mapper, PPU ppu, APU apu, CpuRegisters cpuRegisters, Address initAddress)
            : base(
                new Ram(new AddressRange(0x0000, 0x1fff), 0x0800), // 0x0000
                ppu, //PPU 0x2000
                apu, //APU 0x4000
                mapper, // 0x6000
                mapper, // 0x8000
                mapper, // 0xA000
                mapper, // 0xC000
                mapper) // 0xE000
        {
            this.ppu = ppu;
            this.apu = apu;
            this.cpu = new Ricoh2AFunctional(this, cpuRegisters, initAddress);
            this.cpu.InstructionTrace += OnInstructionTrace;
        }

        private void OnInstructionTrace(InstructionTrace obj)
        {
            OnInstruction?.Invoke(obj);
        }

        public NesBus(IMapper mapper, IInputDevice input1 = null, IInputDevice input2 = null) :
            this(mapper, new PPU(mapper), new APU(input1, input2))
        {
        }

        public Memory<int> FrameBuffer => ppu.FrameBuffer;
        public ReadOnlyMemory<byte> Nametable => ppu.Nametable;
        public ReadOnlyMemory<byte> PatternTable => ppu.PatternTable;

        public PPU PPU => ppu;

        public event Action<Trace> OnCycle;
        public event Action<InstructionTrace> OnInstruction;


        public void Reset()
        {
            this.cpu.Reset();
            timer.Restart();
        }

        public void Run()
        {
            for (long ticks = 0; ; ticks += 12)
            {
                DoCycle();

                if (ticks % Crystal == 0)
                {
                    var delay = TimeSpan.FromSeconds(1) - timer.Elapsed;

                    if (delay > TimeSpan.Zero)
                    {
                        //Console.WriteLine($"CPU Delay {delay}");
                        Thread.Sleep(delay);
                    }
                }
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