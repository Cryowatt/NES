using NES.CPU;
using NES.CPU.Mappers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NES
{
    class Program
    {
        private const long masterClock = 236_250_000 / 11;
        private const double cpuClock = masterClock / 12.0;
        private static int instructionCount = 1;
        private static int skip = 0;

        public class TestMapper : IMapper
        {
            public AddressRange AddressRange { get; } = new AddressRange(0x6000, 0xffff);

            public byte Read(Address address)
            {
                if (address == 0x6ffc)
                {
                    return 0x4c;
                }
                else if (address == 0x6ffd)
                {
                    return 0x00;
                }
                else if (address == 0x6ffe)
                {
                    return 0x60;
                }
                else
                {
                    return 0x80;
                }
            }

            public void Write(Address address, byte value)
            {
                throw new NotImplementedException();
            }
        }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                skip = int.Parse(args[0]);
            }
            var runCycles = 26559;
            RomImage romFile = LoadRom();
            var mapper = new Mapper0(romFile);

            // ========= NOP shit ============
            //var mapper = new TestMapper();
            //var runCycles = (int)cpuClock;

            BothCpu(romFile, runCycles);
            Funccpu(mapper, runCycles);
            Statecpu(mapper, runCycles);
            TotalFuncTime = TimeSpan.Zero;
            TotalStateTime = TimeSpan.Zero;

            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("State Machine CPU");
                Statecpu(mapper, runCycles);
                Console.WriteLine("Functional CPU");
                Funccpu(mapper, runCycles);
            }

            Console.WriteLine("Total Func: " + TotalFuncTime);
            Console.WriteLine("Total Stat: " + TotalStateTime);
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
        private static TimeSpan TotalStateTime = TimeSpan.Zero;

        private static void Statecpu(IMapper mapper, int runCycles)
        {
            instructionCount = 1;

            var bus = new NesBus(mapper);
            bus.Write(0x6001, 0xc0);
            var cpu = new Ricoh2A(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpu.InstructionTrace += OnInstructionTrace;
            var process = cpu.Process();
            var timer = Stopwatch.StartNew();
            foreach (var cycle in process.Take(runCycles))
            {
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpu.CycleCount, cycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalStateTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

        private static void BothCpu(RomImage rom, int runCycles)
        {
            instructionCount = 1;
            var funcBus = new NesBus(new Mapper0(rom));
            var stateBus = new NesBus(new Mapper0(rom));
            funcBus.Write(0x6001, 0xc0);
            stateBus.Write(0x6001, 0xc0);
            var cpus = new Ricoh2A(stateBus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            var cpuf = new Ricoh2AFunctional(funcBus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
            cpus.InstructionTrace += OnInstructionTrace;
            cpuf.InstructionTrace += OnInstructionTracez;
            var process = cpus.Process();
            cpuf.Reset();
            var timer = Stopwatch.StartNew();
            foreach (var cycle in process.Take(runCycles))
            {
                var fcycle = cpuf.DoCycle();
                if (instructionCount + 20 > skip)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpus.CycleCount, cycle);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpuf.CycleCount, fcycle);
                    Console.ResetColor();
                }
            }
            timer.Stop();
            TotalStateTime += timer.Elapsed;
            Console.WriteLine($"{runCycles} in {timer.Elapsed} actual. Relative speed: {runCycles / (timer.Elapsed.TotalSeconds * cpuClock):P}");
        }

        private static void Funccpu(IMapper mapper, int runCycles)
        {
            instructionCount = 1;

            var bus = new NesBus(mapper);
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
