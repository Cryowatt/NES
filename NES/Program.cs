using NES.CPU;
using NES.CPU.Mappers;
using System;
using System.IO;
using System.Linq;

namespace NES
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var reader = new BinaryReader(File.OpenRead(@"C:\Users\ecarter\source\repos\Cryowatt\NES\NES.CPU.Tests\TestRoms\nestest.nes")))
            {
                var romFile = RomImage.From(reader);
                var bus = new NesBus(new Mapper0(romFile));
                var cpu = new Ricoh2A(bus);
                cpu.InstructionTrace += OnInstructionTrace;
                var process = cpu.Process();
                foreach (var cycle in process.SkipWhile((o, i) =>
                {
                    cpu.JMP(0xC000);
                    return i < 1;
                }
                    ).Take(26554))
                {
                    //Console.WriteLine(cycle);
                }
            }
        }

        private static void OnInstructionTrace(InstructionTrace trace)
        {
            Console.WriteLine(trace);
        }
    }
}
