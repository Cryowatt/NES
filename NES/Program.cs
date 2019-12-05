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
        private static int instructionCount = 1;
        private static int skip = 0;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                skip = int.Parse(args[0]);
            }

            using (var reader = new BinaryReader(File.OpenRead(@"C:\Users\ecarter\source\repos\Cryowatt\NES\NES.CPU.Tests\TestRoms\nestest.nes")))
            {
                var romFile = RomImage.From(reader);
                var bus = new NesBus(new Mapper0(romFile));
                bus.Write(0x6001, 0xc0);
                var cpu = new Ricoh2A(bus, new CpuRegisters(StatusFlags.InterruptDisable | StatusFlags.Undefined_6), 0x6000);
                cpu.InstructionTrace += OnInstructionTrace;
                var process = cpu.Process();
                var timer = Stopwatch.StartNew();
                foreach (var cycle in process.Take(26559))
                {
                    if (instructionCount + 20 > skip)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("[{0}, {1}] {2}", instructionCount, cpu.CycleCount, cycle);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine(timer.Elapsed);
            }
        }

        private static void OnInstructionTrace(InstructionTrace trace)
        {
            if (instructionCount + 20 > skip)
            {
                Console.WriteLine("[{0}] {1}", instructionCount, trace);
            }
            instructionCount++;
            if (instructionCount > skip)
            {
                //Console.ReadLine();
            }
        }
    }
}
