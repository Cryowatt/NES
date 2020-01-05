using NES.CPU;
using NES.CPU.Mappers;
using System;
using System.Diagnostics;
using System.IO;

namespace NES
{

    class Program
    {
        private const long masterClock = 236_250_000 / 11;
        private const double cpuClock = masterClock / 12.0;
        private static int instructionCount = 1;
        private static int skip = 0;

        static void Main(string[] args)
        {
            var window = new NesWindow();
            window.Run();

            //RunBasic();

            //if (args.Length > 0)
            //{
            //    skip = int.Parse(args[0]);
            //}
            //var runCycles = 26559;
            //RomImage romFile = LoadRom();
            //var mapper = new Mapper0(romFile);

            //// ========= NOP shit ============
            ////var mapper = new TestMapper();
            ////var runCycles = (int)cpuClock;

            ////BothCpu(romFile, runCycles);
            //Funccpu(mapper, runCycles);
            ////Statecpu(mapper, runCycles);
            //TotalFuncTime = TimeSpan.Zero;
            //TotalStateTime = TimeSpan.Zero;

            //for (int i = 0; i < 20; i++)
            //{
            //    //Console.WriteLine("State Machine CPU");
            //    //Statecpu(mapper, runCycles);
            //    Console.WriteLine("Functional CPU");
            //    Funccpu(mapper, runCycles);
            //}

            //Console.WriteLine("Total Func: " + TotalFuncTime);
            //Console.WriteLine("Total Stat: " + TotalStateTime);
        }

        //private unsafe static void UpdateFrameBuffer()
        //{
        //    using (var pin = platform.FrameBuffer.Pin())
        //    {
        //        fixed (byte* pFrameBuffer = frameBuffer)
        //        {
        //            int* pFrameBuffer = (int*)pin.Pointer;
        //            //int offset = (int)frame; //random.Next();
        //            int* ptr = (int*)pFrameBuffer;

        //            for (int i = 0; i < 256 * 240; i++)
        //            {
        //                *(ptr++) = (((i & 0x1) * 0xff) << 8) | 0xff;
        //            }
        //        }
        //    }
        //}

        private static void RunBasic()
        {
            //Console.BufferWidth
            RomImage romFile;
            var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\01-basics.nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            var mapper = new Mapper0(romFile);
            var platform = new NesBus(mapper);
            platform.Reset();
            platform.Run();
            //var bus = new NesBus(mapper);
            //var cpu = new Ricoh2AFunctional(bus);
            //cpu.Reset();

            //while (true)
            //{
            //    var cycle = platform.DoCycle();
            //}
        }

        private static RomImage LoadRom()
        {
            RomImage romFile;
            Console.WriteLine(Environment.CurrentDirectory);
            var stream = File.OpenRead(@"..\NES.CPU.Tests\TestRoms\nestest.nes");
            using (var reader = new BinaryReader(stream))
            {
                romFile = RomImage.From(reader);
            }

            return romFile;
        }

        private static TimeSpan TotalFuncTime = TimeSpan.Zero;

        private static void Funccpu(IMapper mapper, int runCycles)
        {
            instructionCount = 1;
            var ppu = new PPU(mapper);
            var apu = new APU();
            var bus = new NesBus(mapper, ppu, apu);
            bus.Write(0x6001, 0xc0);
            var cpu = new Ricoh2AFunctional(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpu.InstructionTrace += OnInstructionTrace;
            //var process = cpu.Process();
            var timer = Stopwatch.StartNew();
            cpu.Reset();
            while (cpu.CycleCount < runCycles)
            {
                var cycle = cpu.DoCycle();
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpu.CycleCount, cycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalFuncTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

        private static void OnInstructionTrace(InstructionTrace trace)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (instructionCount + 20 > skip)
            {
                Console.WriteLine("[{0}] {1}", instructionCount, trace);
            }
            instructionCount++;
            if (instructionCount > skip)
            {
                Console.ReadLine();
            }
            Console.ResetColor();
        }

        private static void OnInstructionTracez(InstructionTrace trace)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (instructionCount + 20 > skip)
            {
                Console.WriteLine("[{0}] {1}", instructionCount, trace);
            }
            Console.ResetColor();
        }
    }
}
